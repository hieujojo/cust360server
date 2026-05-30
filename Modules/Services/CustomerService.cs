using MongoDB.Driver;
using MongoDB.Bson;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Exceptions;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepo;
    private readonly IUserRepository _userRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IAuditLogService _auditLogService;
    private readonly ICustomerCodeGenerator _codeGenerator;
    private readonly AtlasSearchService _searchService;
    private readonly CurrentUser _currentUser;

    public CustomerService(
        ICustomerRepository customerRepo,
        IUserRepository userRepo,
        IDepartmentRepository departmentRepo,
        IAuditLogService auditLogService,
        ICustomerCodeGenerator codeGenerator,
        AtlasSearchService searchService,
        CurrentUser currentUser)
    {
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _departmentRepo = departmentRepo;
        _auditLogService = auditLogService;
        _codeGenerator = codeGenerator;
        _searchService = searchService;
        _currentUser = currentUser;
    }

    // ─── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<CustomerResponse>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        ValidateCustomerData(request.Name, request.Source, request.Email, request.Phone);

        // Resolve owner: chỉ Role <= 2 mới được chỉ định owner khác
        string resolvedOwnerId;
        string resolvedDepartmentId;

        if (!string.IsNullOrWhiteSpace(request.OwnerId) && request.OwnerId != _currentUser.UserId)
        {
            if (_currentUser.Role > 2)
                throw new ForbiddenException("Bạn không có quyền tạo khách hàng cho nhân viên khác.");

            var requestedOwner = await _userRepo.FindByIdAsync(request.OwnerId, ct);
            if (requestedOwner == null || requestedOwner.organizationId != _currentUser.OrganizationId)
                return ServiceResult<CustomerResponse>.Fail("INVALID_OWNER", "Owner không hợp lệ hoặc không thuộc tổ chức.");

            resolvedOwnerId = requestedOwner.id;
            resolvedDepartmentId = requestedOwner.departmentId ?? string.Empty;
        }
        else
        {
            resolvedOwnerId = _currentUser.UserId;
            resolvedDepartmentId = _currentUser.DepartmentId ?? string.Empty;
        }

        // Sinh mã code
        var customerCode = await _codeGenerator.GenerateAsync(ct);

        var customer = new Customer
        {
            organizationId = _currentUser.OrganizationId,
            customerCode = customerCode,
            name = request.Name.Trim(),
            source = request.Source,
            email = request.Email?.Trim(),
            phone = request.Phone?.Trim(),
            ownerId = resolvedOwnerId,
            departmentId = resolvedDepartmentId,
            status = "Lead", // Default status
            customFields = request.CustomFields?.ToBsonDocument(),
            contacts = request.Contacts?.Select(c => new Contact
            {
                name = c.Name.Trim(),
                role = c.Role?.Trim(),
                email = c.Email?.Trim(),
                phone = c.Phone?.Trim(),
                isPrimary = c.IsPrimary,
                createdAt = DateTime.UtcNow
            }).ToList() ?? new List<Contact>(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _customerRepo.InsertAsync(customer, ct);

        // Audit Log
        await _auditLogService.LogAsync(
            action: "CUSTOMER_CREATED",
            metadata: new Dictionary<string, string>
            {
                { "customerId", customer.id },
                { "customerCode", customerCode },
                { "ownerId", resolvedOwnerId }
            },
            ct: ct);

        return await GetByIdAsync(customer.id, ct);
    }

    public async Task<ServiceResult<CustomerResponse>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var customer = await _customerRepo.FindByIdAsync(id, ct);
        if (customer == null)
            return ServiceResult<CustomerResponse>.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        // Check department scoping cho Role 3
        if (_currentUser.Role == 3 && customer.departmentId != _currentUser.DepartmentId)
        {
            throw new ForbiddenException("Bạn không có quyền truy cập khách hàng của phòng ban khác.");
        }

        var owner = await _userRepo.FindByIdAsync(customer.ownerId, ct);
        var dept = string.IsNullOrEmpty(customer.departmentId) ? null : await _departmentRepo.FindByIdAsync(customer.departmentId, ct);

        return ServiceResult<CustomerResponse>.Ok(customer.ToResponse(owner, dept));
    }

    public async Task<ServiceResult<CustomerResponse>> UpdateAsync(string id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        ValidateCustomerData(request.Name, request.Source, request.Email, request.Phone, isUpdate: true);

        var customer = await _customerRepo.FindByIdAsync(id, ct);
        if (customer == null)
            return ServiceResult<CustomerResponse>.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        if (_currentUser.Role == 3 && customer.departmentId != _currentUser.DepartmentId)
            throw new ForbiddenException("Bạn không có quyền cập nhật khách hàng của phòng ban khác.");

        var update = Builders<Customer>.Update.Set(c => c.updatedAt, DateTime.UtcNow);

        var changedFields = new List<string>();

        if (request.Name != null) { update = update.Set(c => c.name, request.Name.Trim()); changedFields.Add("name"); }
        if (request.Source != null) { update = update.Set(c => c.source, request.Source); changedFields.Add("source"); }
        if (request.Email != null) { update = update.Set(c => c.email, request.Email.Trim()); changedFields.Add("email"); }
        if (request.Phone != null) { update = update.Set(c => c.phone, request.Phone.Trim()); changedFields.Add("phone"); }
        
        // OwnerId/DepartmentId changes allowed for Role <= 2 only
        if (_currentUser.Role <= 2)
        {
            if (request.OwnerId != null)
            {
                var newOwner = await _userRepo.FindByIdAsync(request.OwnerId, ct);
                if (newOwner == null || newOwner.organizationId != _currentUser.OrganizationId)
                    return ServiceResult<CustomerResponse>.Fail("INVALID_OWNER", "Owner không hợp lệ hoặc không thuộc tổ chức.");
                
                update = update.Set(c => c.ownerId, request.OwnerId);
                changedFields.Add("ownerId");
            }

            if (request.DepartmentId != null)
            {
                var newDept = await _departmentRepo.FindByIdAsync(request.DepartmentId, ct);
                if (newDept == null)
                    return ServiceResult<CustomerResponse>.Fail("INVALID_DEPARTMENT", "Phòng ban không tồn tại.");
                
                update = update.Set(c => c.departmentId, request.DepartmentId);
                changedFields.Add("departmentId");
            }
        }
        else if (request.OwnerId != null || request.DepartmentId != null)
        {
            throw new ForbiddenException("Bạn không có quyền thay đổi Owner hoặc Department.");
        }

        if (request.CustomFields != null)
        {
            update = update.Set(c => c.customFields, request.CustomFields.ToBsonDocument());
            changedFields.Add("customFields");
        }

        if (changedFields.Any())
        {
            await _customerRepo.UpdateAsync(id, update, ct);

            await _auditLogService.LogAsync(
                action: "CUSTOMER_UPDATED",
                metadata: new Dictionary<string, string>
                {
                    { "customerId", id },
                    { "changedFields", string.Join(",", changedFields) }
                },
                ct: ct);
        }

        return await GetByIdAsync(id, ct);
    }

    public async Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        // Controllers should restrict this to Role <= 2, but double check here
        if (_currentUser.Role == 3)
            throw new ForbiddenException("Chỉ Admin hoặc Owner mới được xóa khách hàng.");

        var deleted = await _customerRepo.SoftDeleteAsync(id, ct);
        if (!deleted)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        await _auditLogService.LogAsync(
            action: "CUSTOMER_DELETED",
            metadata: new Dictionary<string, string> { { "customerId", id } },
            ct: ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RestoreAsync(string id, CancellationToken ct = default)
    {
        // Require Role 1 (Owner)
        if (_currentUser.Role != 1)
            throw new ForbiddenException("Chỉ Owner mới được khôi phục khách hàng.");

        var restored = await _customerRepo.RestoreAsync(id, ct);
        if (!restored)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy khách hàng hoặc khách hàng chưa bị xóa.");

        await _auditLogService.LogAsync(
            action: "CUSTOMER_RESTORED",
            metadata: new Dictionary<string, string> { { "customerId", id } },
            ct: ct);

        return ServiceResult.Ok();
    }

    // ─── List & Search ────────────────────────────────────────────────────────

    public async Task<CustomerListResponse> GetListAsync(CustomerListFilterRequest filter, CancellationToken ct = default)
    {
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 20;

        var validStatuses = new[] { "Lead", "Active", "Inactive", "Churned" };
        if (!string.IsNullOrWhiteSpace(filter.Status) && !validStatuses.Contains(filter.Status))
            throw new ValidationException("status", "Status phải là: Lead, Active, Inactive, Churned.");

        var (items, total) = await _customerRepo.FindPagedAsync(
            status: filter.Status,
            ownerId: filter.OwnerId,
            phone: filter.Phone,
            sortBy: filter.SortBy,
            sortDir: filter.SortDir,
            page: filter.Page,
            pageSize: filter.PageSize,
            ct: ct);

        // Fetch owners to populate names
        var ownerIds = items.Select(c => c.ownerId).Distinct().Where(id => !string.IsNullOrEmpty(id));
        var owners = new Dictionary<string, User>();
        
        foreach(var ownerId in ownerIds)
        {
            var user = await _userRepo.FindByIdAsync(ownerId, ct);
            if (user != null) owners[ownerId] = user;
        }

        var responseItems = items.ToSummaryResponseList(id => owners.GetValueOrDefault(id));

        return new CustomerListResponse
        {
            Items = responseItems,
            Pagination = new PaginationMetadata
            {
                CurrentPage = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = total,
                TotalPages = filter.PageSize > 0 ? (int)Math.Ceiling((double)total / filter.PageSize) : 0,
                HasNext = filter.Page * filter.PageSize < total,
                HasPrevious = filter.Page > 1
            }
        };
    }

    public async Task<CustomerSearchResponse> SearchAsync(string query, CancellationToken ct = default)
    {
        return await _searchService.SearchAsync(query, 50, ct);
    }

    // ─── 360 View ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<Customer360ViewResponse>> Get360ViewAsync(string id, CancellationToken ct = default)
    {
        var customer = await _customerRepo.FindByIdAsync(id, ct);
        if (customer == null)
            return ServiceResult<Customer360ViewResponse>.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        if (_currentUser.Role == 3 && customer.departmentId != _currentUser.DepartmentId)
            throw new ForbiddenException("Bạn không có quyền truy cập khách hàng của phòng ban khác.");

        var owner = await _userRepo.FindByIdAsync(customer.ownerId, ct);
        var dept = string.IsNullOrEmpty(customer.departmentId) ? null : await _departmentRepo.FindByIdAsync(customer.departmentId, ct);

        var infoTab = customer.ToInfoTab(owner, dept);
        
        var response = new Customer360ViewResponse
        {
            Info = infoTab,
            Tabs = new Customer360TabsResponse
            {
                Contacts = customer.contacts.ToResponseList(),
                Deals = [],
                Timeline = [],
                Tickets = []
            },
            Sidebar = new Customer360SidebarResponse
            {
                QuickActions = new List<QuickActionResponse>
                {
                    new() { Id = "edit_profile", Label = "Sửa hồ sơ", Icon = "edit", Action = "EDIT_PROFILE" },
                    new() { Id = "change_status", Label = "Đổi trạng thái", Icon = "refresh", Action = "CHANGE_STATUS" },
                    new() { Id = "add_contact", Label = "Thêm người liên hệ", Icon = "user_add", Action = "ADD_CONTACT" }
                },
                OpenDealsCount = 0,
                ActiveTicketsCount = 0
            }
        };

        if (_currentUser.Role <= 2)
        {
            response.Sidebar.QuickActions.Add(new QuickActionResponse { Id = "change_owner", Label = "Đổi Owner", Icon = "swap", Action = "CHANGE_OWNER" });
        }

        return ServiceResult<Customer360ViewResponse>.Ok(response);
    }

    // ─── Status & Owner ───────────────────────────────────────────────────────

    public async Task<ServiceResult> UpdateStatusAsync(string id, UpdateCustomerStatusRequest request, CancellationToken ct = default)
    {
        var customer = await _customerRepo.FindByIdAsync(id, ct);
        if (customer == null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        if (_currentUser.Role == 3 && customer.departmentId != _currentUser.DepartmentId)
            throw new ForbiddenException("Bạn không có quyền cập nhật trạng thái khách hàng của phòng ban khác.");

        var oldStatus = customer.status;
        var newStatus = request.NewStatus;

        if (oldStatus == newStatus)
            return ServiceResult.Ok();

        // Status Transition Matrix
        bool validTransition = (oldStatus, newStatus) switch
        {
            ("Lead", "Active") => true,
            ("Lead", "Inactive") => true,
            ("Active", "Inactive") => true,
            ("Active", "Churned") => true,
            ("Inactive", "Active") => true,
            _ => false
        };

        if (!validTransition)
            throw new InvalidStatusTransitionException(oldStatus, newStatus);

        var update = Builders<Customer>.Update
            .Set(c => c.status, newStatus)
            .Set(c => c.updatedAt, DateTime.UtcNow);

        await _customerRepo.UpdateAsync(id, update, ct);

        await _auditLogService.LogAsync(
            action: "CUSTOMER_STATUS_CHANGED",
            metadata: new Dictionary<string, string>
            {
                { "customerId", id },
                { "oldStatus", oldStatus },
                { "newStatus", newStatus }
            },
            ct: ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateOwnerAsync(string id, UpdateCustomerOwnerRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewOwnerId))
            throw new ValidationException("ownerId", "Owner ID là bắt buộc.");

        var customer = await _customerRepo.FindByIdAsync(id, ct);
        if (customer == null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        if (_currentUser.Role == 3)
            throw new ForbiddenException("Chỉ Admin hoặc Owner mới được đổi Owner của khách hàng.");

        var newOwner = await _userRepo.FindByIdAsync(request.NewOwnerId, ct);
        if (newOwner == null || newOwner.organizationId != _currentUser.OrganizationId)
            return ServiceResult.Fail("INVALID_OWNER", "Owner không hợp lệ hoặc không thuộc tổ chức.");

        var oldOwnerId = customer.ownerId;
        
        if (oldOwnerId == request.NewOwnerId)
            return ServiceResult.Ok();

        var update = Builders<Customer>.Update
            .Set(c => c.ownerId, request.NewOwnerId)
            // Tự động gán customer về department của owner mới (nếu owner mới có department)
            .Set(c => c.departmentId, newOwner.departmentId ?? string.Empty)
            .Set(c => c.updatedAt, DateTime.UtcNow);

        await _customerRepo.UpdateAsync(id, update, ct);

        await _auditLogService.LogAsync(
            action: "CUSTOMER_OWNER_CHANGED",
            metadata: new Dictionary<string, string>
            {
                { "customerId", id },
                { "oldOwnerId", oldOwnerId },
                { "newOwnerId", request.NewOwnerId }
            },
            ct: ct);

        return ServiceResult.Ok();
    }

    // ─── Contacts ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<ContactResponse>> AddContactAsync(string customerId, CreateContactRequest request, CancellationToken ct = default)
    {
        ValidateContactData(request.Name, request.Role, request.Email, request.Phone);

        var customer = await _customerRepo.FindByIdAsync(customerId, ct);
        if (customer == null)
            return ServiceResult<ContactResponse>.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        if (_currentUser.Role == 3 && customer.departmentId != _currentUser.DepartmentId)
            throw new ForbiddenException("Bạn không có quyền thao tác trên khách hàng của phòng ban khác.");

        if (customer.contacts.Count >= 50)
            throw new MaxContactsExceededException();

        var contact = new Contact
        {
            name = request.Name.Trim(),
            role = request.Role?.Trim(),
            email = request.Email?.Trim(),
            phone = request.Phone?.Trim(),
            isPrimary = request.IsPrimary,
            createdAt = DateTime.UtcNow
        };

        if (contact.isPrimary)
        {
            await _customerRepo.ResetAllContactsPrimaryAsync(customerId, ct);
        }

        await _customerRepo.AddContactAsync(customerId, contact, ct);

        await _auditLogService.LogAsync(
            action: "CONTACT_ADDED",
            metadata: new Dictionary<string, string>
            {
                { "customerId", customerId },
                { "contactId", contact.id }
            },
            ct: ct);

        return ServiceResult<ContactResponse>.Ok(contact.ToResponse());
    }

    public async Task<ServiceResult<ContactResponse>> UpdateContactAsync(string customerId, string contactId, CreateContactRequest request, CancellationToken ct = default)
    {
        ValidateContactData(request.Name, request.Role, request.Email, request.Phone);

        var customer = await _customerRepo.FindByIdAsync(customerId, ct);
        if (customer == null)
            return ServiceResult<ContactResponse>.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        if (_currentUser.Role == 3 && customer.departmentId != _currentUser.DepartmentId)
            throw new ForbiddenException("Bạn không có quyền thao tác trên khách hàng của phòng ban khác.");

        var existingContact = customer.contacts.FirstOrDefault(c => c.id == contactId);
        if (existingContact == null)
            return ServiceResult<ContactResponse>.Fail("CONTACT_NOT_FOUND", "Không tìm thấy người liên hệ.");

        if (request.IsPrimary && !existingContact.isPrimary)
        {
            await _customerRepo.ResetAllContactsPrimaryAsync(customerId, ct);
        }

        var update = Builders<Customer>.Update
            .Set("contacts.$.name", request.Name.Trim())
            .Set("contacts.$.role", request.Role?.Trim())
            .Set("contacts.$.email", request.Email?.Trim())
            .Set("contacts.$.phone", request.Phone?.Trim())
            .Set("contacts.$.isPrimary", request.IsPrimary)
            .Set(c => c.updatedAt, DateTime.UtcNow);

        await _customerRepo.UpdateContactAsync(customerId, contactId, update, ct);

        // Fetch updated contact to return
        var updatedCustomer = await _customerRepo.FindByIdAsync(customerId, ct);
        var updatedContact = updatedCustomer?.contacts.FirstOrDefault(c => c.id == contactId);

        return ServiceResult<ContactResponse>.Ok(updatedContact!.ToResponse());
    }

    public async Task<ServiceResult> RemoveContactAsync(string customerId, string contactId, CancellationToken ct = default)
    {
        var customer = await _customerRepo.FindByIdAsync(customerId, ct);
        if (customer == null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        if (_currentUser.Role == 3 && customer.departmentId != _currentUser.DepartmentId)
            throw new ForbiddenException("Bạn không có quyền thao tác trên khách hàng của phòng ban khác.");

        var removed = await _customerRepo.RemoveContactAsync(customerId, contactId, ct);
        if (!removed)
            return ServiceResult.Fail("CONTACT_NOT_FOUND", "Không tìm thấy người liên hệ.");

        await _auditLogService.LogAsync(
            action: "CONTACT_REMOVED",
            metadata: new Dictionary<string, string>
            {
                { "customerId", customerId },
                { "contactId", contactId }
            },
            ct: ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetPrimaryContactAsync(string customerId, string contactId, CancellationToken ct = default)
    {
        var customer = await _customerRepo.FindByIdAsync(customerId, ct);
        if (customer == null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy khách hàng.");

        if (_currentUser.Role == 3 && customer.departmentId != _currentUser.DepartmentId)
            throw new ForbiddenException("Bạn không có quyền thao tác trên khách hàng của phòng ban khác.");

        var existingContact = customer.contacts.FirstOrDefault(c => c.id == contactId);
        if (existingContact == null)
            return ServiceResult.Fail("CONTACT_NOT_FOUND", "Không tìm thấy người liên hệ.");

        if (existingContact.isPrimary)
            return ServiceResult.Ok(); // Đã là primary rồi

        await _customerRepo.ResetAllContactsPrimaryAsync(customerId, ct);
        await _customerRepo.SetContactPrimaryAsync(customerId, contactId, ct);

        return ServiceResult.Ok();
    }

    // ─── Validation Helpers ───────────────────────────────────────────────────

    private void ValidateCustomerData(string? name, string? source, string? email, string? phone, bool isUpdate = false)
    {
        var errors = new Dictionary<string, string[]>();

        if (!isUpdate || name != null)
        {
            if (string.IsNullOrWhiteSpace(name))
                errors.Add("Name", ["Tên khách hàng là bắt buộc."]);
            else if (name.Length > 200)
                errors.Add("Name", ["Tên khách hàng tối đa 200 ký tự."]);
        }

        if (!isUpdate || source != null)
        {
            var validSources = new[] { "Website", "Referral", "Cold Call", "Event", "Partner", "Other" };
            if (source == null || !validSources.Contains(source))
                errors.Add("Source", ["Source không hợp lệ."]);
        }

        if (!string.IsNullOrWhiteSpace(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            errors.Add("Email", ["Email không đúng định dạng."]);

        if (phone?.Length > 20)
            errors.Add("Phone", ["Số điện thoại tối đa 20 ký tự."]);

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    private void ValidateContactData(string? name, string? role, string? email, string? phone)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name", ["Tên người liên hệ là bắt buộc."]);
        else if (name.Length > 200)
            errors.Add("Name", ["Tên người liên hệ tối đa 200 ký tự."]);

        if (role?.Length > 100)
            errors.Add("Role", ["Vai trò tối đa 100 ký tự."]);

        if (!string.IsNullOrWhiteSpace(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            errors.Add("Email", ["Email không đúng định dạng."]);

        if (phone?.Length > 20)
            errors.Add("Phone", ["Số điện thoại tối đa 20 ký tự."]);

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}