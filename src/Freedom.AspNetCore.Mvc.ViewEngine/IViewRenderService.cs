using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Freedom.AspNetCore.Mvc.ViewEngine;

public interface IViewRenderService
{
    Task<string> RenderView(string viewName, object model);
    Task<string> RenderView(string viewName, object model, ViewDataDictionary viewData);
}