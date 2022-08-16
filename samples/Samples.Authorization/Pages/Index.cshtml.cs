using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthorizationSample.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(ILogger<IndexModel> logger, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        _logger = logger;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public void OnGet()
    {

    }

    public async Task<IActionResult> OnPost([FromForm] FormInfo info)
    {
        var identity = HttpContext.User.Identity;
        if (identity != null)
        {
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }
            var user = await _userManager.FindByNameAsync(identity.Name);
            if (info.Do == "add")
            {
                await _userManager.AddToRoleAsync(user, "User");
            }
            else if (info.Do == "remove")
            {
                await _userManager.RemoveFromRoleAsync(user, "User");
            }
        }
        return RedirectToAction("Index");
    }

    public class FormInfo
    {
        public string? Do { get; set; }
    }
}
