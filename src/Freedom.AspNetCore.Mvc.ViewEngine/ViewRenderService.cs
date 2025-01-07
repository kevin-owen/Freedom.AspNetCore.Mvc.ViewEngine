using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Freedom.AspNetCore.Mvc.ViewEngine;

public class ViewRenderService(IServiceProvider serviceProvider, ITempDataProvider tempDataProvider)
    : IViewRenderService
{
    public async Task<string> RenderView(string viewName, object model)
    {
        return await RenderView(viewName, model, null);
    }

    public async Task<string> RenderView(string viewName, object model, ViewDataDictionary? viewData)
    {
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        await using var sw = new StringWriter();

        var view = GetView(httpContext, viewName);
        var viewContext = GetViewContext(actionContext, view, model, sw);
        if (viewData != null)
        {
            foreach (var (key, value) in viewData)
            {
                viewContext.ViewData.Add(key, value);
            }
        }

        await view.RenderAsync(viewContext);

        return sw.ToString();
    }

    private ViewContext GetViewContext(ActionContext actionContext, IView view, object model, StringWriter sw)
    {
        return new ViewContext(actionContext, view, GetViewDataDictionary(model), GetTempDataDictionary(actionContext), sw, new HtmlHelperOptions());
    }

    private static IView GetView(HttpContext httpContext, string viewName)
    {
        var viewResult = GetViewResult(httpContext, viewName);
        if (viewResult?.View == null)
        {
            throw new ArgumentNullException($"{viewName} does not match any available view");
        }

        return viewResult.View;
    }

    private static ViewEngineResult? GetViewResult(HttpContext httpContext, string viewName)
    {
        var viewEngine = GetViewEngine(httpContext);
        var hostingEnvironment = GetHostingEnvironment(httpContext);

        if (viewEngine == null || hostingEnvironment == null)
        {
            return null;
        }

        return viewEngine.GetView(hostingEnvironment.WebRootPath, viewName, false);
    }

    private static ViewDataDictionary GetViewDataDictionary(object model)
    {
        return new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
               {
                   Model = model
               };
    }

    private TempDataDictionary GetTempDataDictionary(ActionContext actionContext)
    {
        return new TempDataDictionary(actionContext.HttpContext, tempDataProvider);
    }

    private static ICompositeViewEngine? GetViewEngine(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
    }

    private static IWebHostEnvironment? GetHostingEnvironment(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
    }
}
