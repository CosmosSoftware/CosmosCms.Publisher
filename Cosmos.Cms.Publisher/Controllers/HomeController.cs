﻿using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Data.Logic;
using Cosmos.Cms.Common.Models;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Publisher.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;

namespace Cosmos.Cms.Publisher.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ArticleLogic _articleLogic;
        private readonly IOptions<CosmosConfig> _options;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, ArticleLogic articleLogic, IOptions<CosmosConfig> options, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _articleLogic = articleLogic;
            _options = options;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var article = await _articleLogic.GetByUrl(HttpContext.Request.Path, HttpContext.Request.Query["lang"], TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20)); // ?? await _articleLogic.GetByUrl(id, langCookie);

                // Article not found?
                // try getting a version not published.

                if (article == null)
                {

                    //
                    // Create your own not found page for a graceful page for users.
                    //
                    article = await _articleLogic.GetByUrl("/not_found", HttpContext.Request.Query["lang"]);

                    if (article == null)
                    {
                        if (await _dbContext.Pages.CosmosAnyAsync() == false)
                        {
                            // No pages published yet
                            return View("UnderConstruction");
                        }
                    }

                    HttpContext.Response.StatusCode = 404;

                    if (article == null) return NotFound();
                }
                
                //if (HttpContext.Request.Query["json"] == true)
                //    return Json(article);

                return View(article);
            }
            catch (Microsoft.Azure.Cosmos.CosmosException e)
            {
                return View("UnderConstruction");                
            }
            catch (Exception e)
            {
                try
                {
                    _logger.LogError(e.Message, e);
                }
                catch { }

                throw;
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Gets the application validation for Microsoft
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public IActionResult GetMicrosoftIdentityAssociation()
        {
            var model = new MicrosoftValidationObject();
            var appIds = _options.Value.MicrosoftAppId.Split(',');

            foreach (var id in appIds)
            {
                model.associatedApplications.Add(new AssociatedApplication() { applicationId = id });
            }

            var data = Newtonsoft.Json.JsonConvert.SerializeObject(model);

            return File(Encoding.UTF8.GetBytes(data), "application/json", fileDownloadName: "microsoft-identity-association.json");
        }


        /// <summary>
        /// Gets the children of a given page path.
        /// </summary>
        /// <param name="page">UrlPath</param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <param name="orderByPub"></param>
        /// <returns></returns>
        [EnableCors("AllCors")]
        public async Task<IActionResult> GetTOC(
            string page,
            bool? orderByPub,
            int? pageNo,
            int? pageSize)
        {
            var result = await _articleLogic.GetTOC(page, pageNo ?? 0, pageSize ?? 10, orderByPub ?? false);
            return Json(result);
        }

    }
}