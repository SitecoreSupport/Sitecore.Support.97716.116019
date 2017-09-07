using Sitecore.Forms.Mvc.Attributes;
using Sitecore.Forms.Mvc.Controllers.Filters;
using Sitecore.Forms.Mvc.Controllers.ModelBinders;
using Sitecore.Forms.Mvc.Interfaces;
using Sitecore.Forms.Mvc.Models;
using Sitecore.Forms.Mvc.ViewModels;
using Sitecore.Mvc.Controllers;
using Sitecore.WFFM.Abstractions;
using System.IO;
using System.Web.Mvc;
using Sitecore.Forms.Mvc.Controllers;

namespace Sitecore.Support.Forms.Mvc.Controllers
{
    [ModelBinder(typeof(FormModelBinder))]
    public class FormController : SitecoreController
    {
        public IRepository<FormModel> FormRepository
        {
            get;
            private set;
        }

        public IAutoMapper<FormModel, FormViewModel> Mapper
        {
            get;
            private set;
        }

        public IFormProcessor<FormModel> FormProcessor
        {
            get;
            private set;
        }

        public FormController() : this((IRepository<FormModel>)Sitecore.Configuration.Factory.CreateObject(Sitecore.Forms.Mvc.Constants.FormRepository, true), (IAutoMapper<FormModel, FormViewModel>)Sitecore.Configuration.Factory.CreateObject(Sitecore.Forms.Mvc.Constants.FormAutoMapper, true), (IFormProcessor<FormModel>)Sitecore.Configuration.Factory.CreateObject(Sitecore.Forms.Mvc.Constants.FormProcessor, true))
        {
        }

        public FormController(IRepository<FormModel> repository, IAutoMapper<FormModel, FormViewModel> mapper, IFormProcessor<FormModel> processor)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(repository, "repository");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(mapper, "mapper");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(processor, "processor");
            this.FormRepository = repository;
            this.Mapper = mapper;
            this.FormProcessor = processor;
        }

        [FormErrorHandler, HttpGet]
        public override ActionResult Index()
        {
            return this.Form();
        }

        [FormErrorHandler, SubmittedFormHandler, WffmValidateAntiForgeryToken, HttpPost]
        public virtual ActionResult Index([ModelBinder(typeof(FormModelBinder))] FormViewModel formViewModel)
        {
            return this.ProcessedForm(formViewModel, "");
        }

        [FormErrorHandler, AllowCrossSiteJson]
        public virtual JsonResult Process([ModelBinder(typeof(FormModelBinder))] FormViewModel formViewModel)
        {
            DependenciesManager.AnalyticsTracker.InitializeTracker();
            Sitecore.Support.Forms.Mvc.Controllers.ProcessedFormResult<FormModel, FormViewModel> processedFormResult = this.ProcessedForm(formViewModel, "~/Views/Form/Index.cshtml");
            processedFormResult.ExecuteResult(base.ControllerContext);
            string data;
            using (StringWriter stringWriter = new StringWriter())
            {
                ViewContext viewContext = new ViewContext(base.ControllerContext, processedFormResult.View, base.ViewData, base.TempData, stringWriter);
                processedFormResult.View.Render(viewContext, stringWriter);
                data = stringWriter.GetStringBuilder().ToString();
            }
            base.ControllerContext.HttpContext.Response.Clear();
            return new JsonResult
            {
                Data = data
            };
        }

        public virtual FormResult<FormModel, FormViewModel> Form()
        {
            return new FormResult<FormModel, FormViewModel>(this.FormRepository, this.Mapper)
            {
                ViewData = base.ViewData,
                TempData = base.TempData,
                ViewEngineCollection = base.ViewEngineCollection
            };
        }

        public virtual Sitecore.Support.Forms.Mvc.Controllers.ProcessedFormResult<FormModel, FormViewModel> ProcessedForm(FormViewModel viewModel, string viewName = "")
        {
            Sitecore.Support.Forms.Mvc.Controllers.ProcessedFormResult<FormModel, FormViewModel> processedFormResult = new Sitecore.Support.Forms.Mvc.Controllers.ProcessedFormResult<FormModel, FormViewModel>(this.FormRepository, this.Mapper, this.FormProcessor, viewModel)
            {
                ViewData = base.ViewData,
                TempData = base.TempData,
                ViewEngineCollection = base.ViewEngineCollection
            };
            if (!string.IsNullOrEmpty(viewName))
            {
                processedFormResult.ViewName = viewName;
            }
            return processedFormResult;
        }
    }
}