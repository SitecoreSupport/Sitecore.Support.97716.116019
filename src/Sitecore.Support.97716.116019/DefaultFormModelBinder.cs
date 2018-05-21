using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Forms.Mvc;
using Sitecore.Forms.Mvc.Controllers;
using Sitecore.Forms.Mvc.Data.Wrappers;
using Sitecore.Forms.Mvc.Models;
using Sitecore.Forms.Mvc.ViewModels;
using System;
using System.Web.Mvc;
using System.Web.SessionState;

namespace Sitecore.Support.Forms.Mvc.Controllers.ModelBinders
{
    public class DefaultFormModelBinder : DefaultModelBinder
    {
        public IRenderingContext RenderingContext
        {
            get;
            private set;
        }

        public DefaultFormModelBinder()
            : this((IRenderingContext)Factory.CreateObject(Sitecore.Forms.Mvc.Constants.FormRenderingContext, true))
        {
        }

        public DefaultFormModelBinder(IRenderingContext renderingContext)
        {
            Assert.ArgumentNotNull(renderingContext, "renderingContext");
            RenderingContext = renderingContext;
        }

        public virtual FormViewModel GetFormViewModel(ControllerContext controllerContext)
        {
            Assert.ArgumentNotNull(controllerContext, "controllerContext");
            if (RenderingContext != null && RenderingContext.Rendering != null)
            {
                Guid uniqueId = RenderingContext.Rendering.UniqueId;
                if (controllerContext.HttpContext.Session != null && controllerContext.HttpContext.Session.Mode == SessionStateMode.InProc)
                {
                    object obj = controllerContext.HttpContext.Session[uniqueId.ToString()];
                    if (obj != null)
                    {
                        return obj as FormViewModel;
                    }
                }
                FormController formController = controllerContext.Controller as FormController;
                if (formController != null)
                {
                    FormModel model = formController.FormRepository.GetModel(uniqueId);
                    if (model != null)
                    {
                        return formController.Mapper.GetView(model);
                    }
                }
                return null;
            }
            return null;
        }

        public virtual string GetPrefix(Guid id)
        {
            if (!(id == Guid.Empty))
            {
                return FormViewModel.GetClientId(id);
            }
            return null;
        }
    }
}