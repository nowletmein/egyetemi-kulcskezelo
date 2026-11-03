using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class RBACTests {
    [Fact]
    public async Task UserController_RequiresAuthenticationByDefault() {
        var source = await LoadControllerSourceAsync("UserController");

        Assert.Matches(
            @"\[Authorize\]\s*public\s+class\s+UserController",
            source);
    }

    [Fact]
    public async Task Create_AllowsAnonymousAccess() {
        var attributes = await GetActionAuthorizationAttributesAsync("Create");

        Assert.Contains(attributes, attribute => attribute.Name == "AllowAnonymous");
    }

    [Fact]
    public async Task Login_AllowsAnonymousAccess() {
        var attributes = await GetActionAuthorizationAttributesAsync("Login");

        Assert.Contains(attributes, attribute => attribute.Name == "AllowAnonymous");
    }

    [Fact]
    public async Task GetAll_RequiresAdminRole() {
        var attributes = await GetActionAuthorizationAttributesAsync("GetAll");

        Assert.DoesNotContain(attributes, attribute => attribute.Name == "AllowAnonymous");
        Assert.Contains("Admin", await GetActionRolesAsync("GetAll"));
    }

    [Fact]
    public async Task GetAllRoles_RequiresAdminRole() {
        var attributes = await GetActionAuthorizationAttributesAsync("GetAllRoles");

        Assert.DoesNotContain(attributes, attribute => attribute.Name == "AllowAnonymous");
        Assert.Contains("Admin", await GetActionRolesAsync("GetAllRoles"));
    }

    [Fact]
    public async Task Get_AllowsRolelessUser() {
        await AssertActionHasNoRoleRequirementAsync("Get");
    }

    [Fact]
    public async Task Update_AllowsRolelessUser() {
        await AssertActionHasNoRoleRequirementAsync("Update");
    }

    [Fact]
    public async Task ChangePassword_AllowsRolelessUser() {
        await AssertActionHasNoRoleRequirementAsync("ChangePassword");
    }

    [Fact]
    public async Task GetInfo_AllowsAdminAndRejectsOktatoRole() {
        await AssertActionHasOnlyRoleAsync("GetInfo", "Admin", "Oktato");
    }

    [Fact]
    public async Task Delete_AllowsAdminAndRejectsPortasRole() {
        await AssertActionHasOnlyRoleAsync("Delete", "Admin", "Portas");
    }

    [Fact]
    public async Task AddRoleToUser_AllowsAdminAndRejectsIgazgatoRole() {
        await AssertActionHasOnlyRoleAsync("AddRoleToUser", "Admin", "Igazgato");
    }

    [Fact]
    public async Task AddRole_AllowsAdminAndRejectsOktatoRole() {
        await AssertActionHasOnlyRoleAsync("AddRole", "Admin", "Oktato");
    }

    [Fact]
    public async Task DeleteRole_AllowsAdminAndRejectsPortasRole() {
        await AssertActionHasOnlyRoleAsync("DeleteRole", "Admin", "Portas");
    }

    [Fact]
    public async Task RemoveFromUser_AllowsAdminAndRejectsIgazgatoRole() {
        await AssertActionHasOnlyRoleAsync("RemoveFromUser", "Admin", "Igazgato");
    }

    [Fact]
    public async Task BuildingController_RequiresAuthenticationByDefault() {
        var source = await LoadControllerSourceAsync("BuildingController");

        Assert.Matches(
            @"\[Authorize\]\s*public\s+class\s+BuildingController",
            source);
    }

    [Fact]
    public async Task BuildingMutations_RequireAdminRole() {
        await AssertActionHasRoleAsync("Create", "Admin", "BuildingController");
        await AssertActionHasRoleAsync("Update", "Admin", "BuildingController");
        await AssertActionHasRoleAsync("Delete", "Admin", "BuildingController");
    }

    [Fact]
    public async Task FloorController_RequiresAuthenticationByDefault() {
        var source = await LoadControllerSourceAsync("FloorController");

        Assert.Matches(
            @"\[Authorize\]\s*public\s+class\s+FloorController",
            source);
    }

    [Fact]
    public async Task FloorMutations_RequireAdminRole() {
        await AssertActionHasRoleAsync("Create", "Admin", "FloorController");
        await AssertActionHasRoleAsync("Update", "Admin", "FloorController");
        await AssertActionHasRoleAsync("Delete", "Admin", "FloorController");
    }

    [Fact]
    public async Task BookingController_RequiresAuthenticationByDefault() {
        var source = await LoadControllerSourceAsync("BookingController");

        Assert.Matches(
            @"\[Authorize\]\s*public\s+class\s+BookingController",
            source);
    }

    [Fact]
    public async Task BookingCreation_AllowsRolelessAndOktatoUsers() {
        var attributes = await GetActionAuthorizationAttributesAsync(
            "AddUserBooking",
            "BookingController");
        var source = await LoadControllerSourceAsync("BookingController");

        Assert.DoesNotContain(
            attributes,
            attribute =>
                attribute.Name == "Authorize" &&
                attribute.Arguments.ContainsKey("Roles"));
        Assert.Contains("!User.IsInRole(\"Oktato\")", source);
        Assert.Contains("claim.Type == ClaimTypes.Role", source);
    }

    private static async Task AssertActionHasNoRoleRequirementAsync(
        string actionName,
        string controllerName = "UserController") {
        var attributes = await GetActionAuthorizationAttributesAsync(
            actionName,
            controllerName);

        Assert.DoesNotContain(
            attributes,
            attribute =>
                attribute.Name == "Authorize" &&
                attribute.Arguments.ContainsKey("Roles"));
    }

    private static async Task AssertActionHasRoleAsync(
        string actionName,
        string expectedRole,
        string controllerName = "UserController") {
        var roles = await GetActionRolesAsync(actionName, controllerName);

        Assert.Contains(expectedRole, roles);
    }

    private static async Task AssertActionHasOnlyRoleAsync(
        string actionName,
        string allowedRole,
        string rejectedRole,
        string controllerName = "UserController") {
        var roles = await GetActionRolesAsync(actionName, controllerName);

        Assert.Equal(new[] { allowedRole }, roles);
        Assert.DoesNotContain(rejectedRole, roles);
    }

    private static async Task<string[]> GetActionRolesAsync(
        string actionName,
        string controllerName = "UserController") {
        var attributes = await GetActionAuthorizationAttributesAsync(
            actionName,
            controllerName);
        var authorizeAttribute = attributes
            .LastOrDefault(attribute => attribute.Name == "Authorize");

        if (authorizeAttribute is null) {
            throw new Xunit.Sdk.XunitException(
                $"The {actionName} action must declare an authorization requirement.");
        }

        if (!authorizeAttribute.Arguments.TryGetValue("Roles", out var roles)) {
            throw new Xunit.Sdk.XunitException(
                $"The {actionName} action must declare an explicit role requirement.");
        }

        return roles.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .OrderBy(role => role)
            .ToArray();
    }

    private static async Task<List<AuthorizationAttribute>>
        GetActionAuthorizationAttributesAsync(
            string actionName,
            string controllerName = "UserController") {
        var source = await LoadControllerSourceAsync(controllerName);
        var method = Regex.Match(
            source,
            $@"\bpublic\s+async\s+Task(?:<[\w<>,? ]+>)?\s+{Regex.Escape(actionName)}\s*\(");
        Assert.True(
            method.Success,
            $"Could not find {controllerName}.{actionName}.");

        var httpAttributeIndex = source.LastIndexOf(
            "[Http",
            method.Index,
            StringComparison.Ordinal);
        Assert.True(
            httpAttributeIndex >= 0,
            $"Could not find an HTTP route for {controllerName}.{actionName}.");

        var actionAttributes = source[httpAttributeIndex..method.Index];
        return Regex.Matches(
                actionAttributes,
                @"\[(?<name>Authorize|AllowAnonymous)(?:\((?<arguments>[^)]*)\))?\]")
            .Select(match => new AuthorizationAttribute(
                match.Groups["name"].Value,
                ParseArguments(match.Groups["arguments"].Value)))
            .ToList();
    }

    private static Dictionary<string, string> ParseArguments(string arguments) =>
        Regex.Matches(arguments, @"(?<name>\w+)\s*=\s*""(?<value>[^""]*)""")
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => match.Groups["value"].Value,
                StringComparer.Ordinal);

    private static async Task<string> LoadControllerSourceAsync(string controllerName) {
        var searchDirectories = new[]
            {
                AppContext.BaseDirectory,
                Environment.CurrentDirectory
            }
            .SelectMany(GetParentDirectories)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in searchDirectories) {
            var controllerPath = Path.Combine(
                directory,
                "KulcsRendszer.API",
                "Controllers",
                $"{controllerName}.cs");
            if (File.Exists(controllerPath)) {
                return await File.ReadAllTextAsync(controllerPath);
            }
        }

        throw new FileNotFoundException(
            $"Could not locate KulcsRendszer.API/Controllers/{controllerName}.cs.");
    }

    private static IEnumerable<string> GetParentDirectories(string path) {
        var directory = new DirectoryInfo(path);
        while (directory != null) {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private sealed record AuthorizationAttribute(
        string Name,
        Dictionary<string, string> Arguments);
}
