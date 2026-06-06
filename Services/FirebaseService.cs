using System.Text.Json;
using CRM.Api.Infrastructure.Settings;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;

namespace CRM.Api.Services;

/// <summary>Firebase Admin + Firestore writes. FirebaseApp được khởi tạo một lần (singleton).</summary>
public sealed class FirebaseService
{
    private static readonly object AppLock = new();
    private readonly FirestoreDb _firestore;
    private readonly FirebaseAuth _firebaseAuth;

    public FirebaseService(IOptions<FirebaseSettings> options, IHostEnvironment env)
    {
        var settings = options.Value;
        var credentialsPath = ResolveCredentialsPath(settings.CredentialsPath, env.ContentRootPath);

        if (!File.Exists(credentialsPath))
            throw new FileNotFoundException($"Firebase credentials not found: {credentialsPath}");

        var credential = GoogleCredential.FromFile(credentialsPath);

        lock (AppLock)
        {
            if (FirebaseApp.DefaultInstance is null)
                FirebaseApp.Create(new AppOptions { Credential = credential });
        }

        _firebaseAuth = FirebaseAuth.DefaultInstance;

        var projectId = ReadProjectId(credentialsPath);
        _firestore = new FirestoreDbBuilder
        {
            ProjectId = projectId,
            Credential = credential,
        }.Build();
    }

    public Task WriteFirestoreDocAsync(string collection, string docId, object data) =>
        _firestore.Collection(collection).Document(docId).SetAsync(data);

    public async Task WriteFirestoreSubDocAsync(string path, object data)
    {
        var parts = path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        if (parts.Length < 2 || parts.Length % 2 != 0)
        {
            throw new ArgumentException(
                "Path must be collection/doc pairs, e.g. notifications/{orgId}/items/{id}.",
                nameof(path)
            );
        }

        var docRef = _firestore.Collection(parts[0]).Document(parts[1]);
        for (var i = 2; i < parts.Length; i += 2)
            docRef = docRef.Collection(parts[i]).Document(parts[i + 1]);

        await docRef.SetAsync(data);
    }

    public async Task<string> CreateCustomTokenAsync(string userId)
    {
        // uid ở đây dùng MongoDB _id của user luôn cũng được
        return await _firebaseAuth.CreateCustomTokenAsync(userId);
    }

    private static string ResolveCredentialsPath(string configuredPath, string contentRoot) =>
        Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(contentRoot, configuredPath);

    private static string ReadProjectId(string credentialsPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(credentialsPath));
        return doc.RootElement.GetProperty("project_id").GetString()
            ?? throw new InvalidOperationException(
                "project_id is missing in Firebase credentials file."
            );
    }
}
