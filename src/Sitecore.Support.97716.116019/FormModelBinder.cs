using Sitecore.Diagnostics;
using Sitecore.Forms.Mvc;
using Sitecore.Forms.Mvc.Controllers.ModelBinders;
using Sitecore.Forms.Mvc.Data.Wrappers;
using System;
using System.Web.Mvc;

namespace Sitecore.Support.Forms.Mvc.Controllers.ModelBinders
{
    public class FormModelBinder : Sitecore.Support.Forms.Mvc.Controllers.ModelBinders.DefaultFormModelBinder
    {
        public FormModelBinder()
        {
        }

        public FormModelBinder(IRenderingContext renderingContext)
            : base(renderingContext)
        {
        }

        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            Assert.ArgumentNotNull(controllerContext, "controllerContext");
            Assert.ArgumentNotNull(bindingContext, "bindingContext");
            string empty = string.Empty;
            Guid empty2 = Guid.Empty;
            if (!base.RenderingContext.Rendering.IsFormRendering)
            {
                return null;
            }
            empty2 = base.RenderingContext.Rendering.UniqueId;
            empty = GetPrefix(empty2);
            if (string.IsNullOrEmpty(empty))
            {
                return null;
            }
            ValueProviderResult value = bindingContext.ValueProvider.GetValue(empty + "." + Sitecore.Forms.Mvc.Constants.Id);
            if (value == null)
            {
                return null;
            }
            if (Guid.TryParse(value.AttemptedValue, out Guid a) && (!(empty2 != Guid.Empty) || !(a != empty2)))
            {
                bindingContext.ModelName = empty;
                return base.BindModel(controllerContext, bindingContext);
            }
            return null;
        }

        protected override object CreateModel(ControllerContext controllerContext, ModelBindingContext bindingContext, Type modelType)
        {
            return GetFormViewModel(controllerContext) ?? base.CreateModel(controllerContext, bindingContext, modelType);
        }
    }
}