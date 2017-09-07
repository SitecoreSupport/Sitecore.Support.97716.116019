using System;
using System.Linq;
using System.Web.Mvc;
using Sitecore.Diagnostics;
using Sitecore.Forms.Mvc.Interfaces;
using Sitecore.Forms.Mvc.Controllers;

namespace Sitecore.Support.Forms.Mvc.Controllers
{
    public class ProcessedFormResult<TFormModel, TFormViewModel> : FormResult<TFormModel, TFormViewModel>
      where TFormModel : class, IModelEntity
      where TFormViewModel : class, IViewModel, IHasId
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="ProcessedFormResult{TFormModel, TFormViewModel}" /> class.
        /// </summary>
        /// <param name="formRepository">The form repository.</param>
        /// <param name="autoMapper">The automatic mapper.</param>
        /// <param name="formProcessor">The form processor.</param>
        /// <param name="viewModel">The view model.</param>
        public ProcessedFormResult([NotNull] IRepository<TFormModel> formRepository, [NotNull] IAutoMapper<TFormModel, TFormViewModel> autoMapper, [NotNull] IFormProcessor<TFormModel> formProcessor, [CanBeNull] TFormViewModel viewModel)
          : base(formRepository, autoMapper)
        {
            Assert.ArgumentNotNull(formProcessor, "formProcessor");

            this.FormProcessor = formProcessor;
            this.ViewModel = viewModel;
        }

        /// <summary>
        ///   Gets the form processor.
        /// </summary>
        /// <value>
        ///   The form processor.
        /// </value>
        [NotNull]
        public IFormProcessor<TFormModel> FormProcessor { get; private set; }

        /// <summary>
        ///   Gets the model.
        /// </summary>
        /// <value>
        ///   The model.
        /// </value>
        [CanBeNull]
        public TFormViewModel ViewModel { get; set; }

        /// <summary>
        ///   Executes the result.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void ExecuteResult(ControllerContext context)
        {
            Assert.ArgumentNotNull(context, "context");

            if (this.ViewModel == null)
            {
                base.ExecuteResult(context);
                return;
            }

            if (!context.Controller.ViewData.ModelState.IsValid)
            {
                this.ViewData.Model = this.ViewModel;
                this.BaseExecuteResult(context);
                return;
            }

            var model = this.FormRepository.GetModel(this.ViewModel.UniqueId);

            Assert.IsNotNull(model, "model");

            this.Mapper.SetModelResults(this.ViewModel, model);

            this.FormProcessor.Run(model);

            var successSubmit = model.IsValid;

            if (model.Failures.Count > 0)
            {
                successSubmit = false;
                var hasErrors = this.ViewModel as IHasErrors;
                if (hasErrors != null)
                {
                    hasErrors.Errors = model.Failures.Select(x => x.ErrorMessage).Distinct().ToList();
                }
            }

            var submitViewModel = this.ViewModel as ISubmitSettings;
            if (submitViewModel != null)
            {
                submitViewModel.SuccessSubmit = successSubmit;
            }

            if (successSubmit && model.RedirectOnSuccess && !string.IsNullOrEmpty(model.SuccessRedirectUrl))
            {
                this.RedirectToSuccessPage(context, model.SuccessRedirectUrl);
                return;
            }

            this.ViewData.Model = submitViewModel;

            this.BaseExecuteResult(context);
        }

        private void RedirectToSuccessPage([NotNull] ControllerContext context, [NotNull] string url)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNullOrEmpty(url, "url");

            if (context.IsChildAction)
            {
                throw new InvalidOperationException("Cannot redirect in child action");
            }

            this.ViewName = "SuccessRedirect";
            this.ViewData.Model = UrlHelper.GenerateContentUrl(url, context.HttpContext);
            this.BaseExecuteResult(context);
        }
    }
}