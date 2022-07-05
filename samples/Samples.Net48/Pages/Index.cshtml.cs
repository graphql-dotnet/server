using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GraphQL.Server.Samples.Net48.Pages;

public class IndexModel : PageModel
{
    [Route("/")]
    public ActionResult OnGet()
    {
        return Redirect("/ui/graphql");
    }
}
