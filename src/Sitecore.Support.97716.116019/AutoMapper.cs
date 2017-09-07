using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Utility;
using Sitecore.Forms.Core.Data;
using Sitecore.Forms.Mvc.Helpers;
using Sitecore.Forms.Mvc.Interfaces;
using Sitecore.Forms.Mvc.Models;
using Sitecore.Forms.Mvc.Reflection;
using Sitecore.Forms.Mvc.ViewModels;
using Sitecore.Mvc.Extensions;
using Sitecore.WFFM.Abstractions.Data;

namespace Sitecore.Support.Forms.Mvc.Services
{
    public class AutoMapper : IAutoMapper<FormModel, FormViewModel>
    {
        /// <summary>
        ///   The form models
        /// </summary>
        /// <summary>
        ///   Gets the view.
        /// </summary>
        /// <param name="modelEntity">The model entity.</param>
        /// <returns></returns>
        public FormViewModel GetView(FormModel modelEntity)
        {
            Assert.ArgumentNotNull(modelEntity, "modelEntity");

            var formViewModel = new FormViewModel();
            formViewModel.UniqueId = modelEntity.UniqueId;
            formViewModel.Information = modelEntity.Item.Introduction ?? string.Empty;
            formViewModel.IsAjaxForm = modelEntity.Item.IsAjaxMvcForm;
            formViewModel.IsSaveFormDataToStorage = modelEntity.Item.IsSaveFormDataToStorage;

            formViewModel.Title = modelEntity.Item.FormName ?? string.Empty;
            formViewModel.TitleTag = modelEntity.Item.TitleTag.ToString();

            formViewModel.ShowTitle = modelEntity.Item.ShowTitle;
            formViewModel.ShowFooter = modelEntity.Item.ShowFooter;
            formViewModel.ShowInformation = modelEntity.Item.ShowIntroduction;
            formViewModel.SubmitButtonName = modelEntity.Item.SubmitName ?? string.Empty;
            formViewModel.SubmitButtonPosition = modelEntity.Item.SubmitButtonPosition ?? string.Empty;
            formViewModel.SubmitButtonSize = modelEntity.Item.SubmitButtonSize ?? string.Empty;
            formViewModel.SubmitButtonType = modelEntity.Item.SubmitButtonType ?? string.Empty;
            formViewModel.SuccessMessage = modelEntity.Item.SuccessMessage ?? string.Empty;
            formViewModel.SuccessSubmit = false;
            formViewModel.Errors = modelEntity.Failures.Select(x => x.ErrorMessage).ToList();
            formViewModel.Visible = true;
            formViewModel.LeftColumnStyle = modelEntity.Item.LeftColumnStyle;
            formViewModel.RightColumnStyle = modelEntity.Item.RightColumnStyle;
            formViewModel.Footer = modelEntity.Item.Footer;
            formViewModel.Item = modelEntity.Item.InnerItem;
            formViewModel.FormType = modelEntity.Item.FormType;
            formViewModel.ReadQueryString = modelEntity.ReadQueryString;
            formViewModel.QueryParameters = modelEntity.QueryParameters;

            //CSS Styles
            var style = new StringBuilder();
            style.Append(modelEntity.Item.FormTypeClass ?? string.Empty).Append(" ").Append(modelEntity.Item.CustomCss ?? string.Empty).
              Append(" ").Append(modelEntity.Item.FormAlignment ?? string.Empty);

            formViewModel.CssClass = style.ToString().Trim();

            ReflectionUtils.SetXmlProperties(formViewModel, modelEntity.Item.Parameters, true);

            formViewModel.Sections = modelEntity.Item.SectionItems.Select(x => this.GetSectionViewModel(new SectionItem(x), formViewModel)).Where(x => x != null).ToList();

            return formViewModel;
        }

        /// <summary>
        ///   Gets the model.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="formModel">The form model.</param>
        /// <returns></returns>
        public void SetModelResults(FormViewModel view, FormModel formModel)
        {
            Assert.ArgumentNotNull(view, "view");
            Assert.ArgumentNotNull(formModel, "formModel");

            formModel.Results = view.Sections.SelectMany(x => x.Fields).Select(x => ((IFieldResult)x).GetResult()).Where(x => x != null && x.Value != null).ToList();
        }

        /// <summary>
        ///   Gets the section view model.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="formViewModel">The form view model.</param>
        /// <returns></returns>
        protected SectionViewModel GetSectionViewModel([NotNull] SectionItem item, FormViewModel formViewModel)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(formViewModel, "formViewModel");

            var sectionViewModel = new SectionViewModel();
            sectionViewModel.Fields = new List<FieldViewModel>();
            sectionViewModel.Item = item.InnerItem;

            var title = item.Title;
            sectionViewModel.Visible = true;

            if (!string.IsNullOrEmpty(title))
            {
                sectionViewModel.ShowInformation = true;
                sectionViewModel.Title = item.Title ?? string.Empty;

                ReflectionUtils.SetXmlProperties(sectionViewModel, item.Parameters, true);

                sectionViewModel.ShowTitle = sectionViewModel.ShowLegend != "No";

                ReflectionUtils.SetXmlProperties(sectionViewModel, item.LocalizedParameters, true);
            }

            sectionViewModel.Fields = item.Fields.Select(x => this.GetFieldViewModel(x, formViewModel)).Where(x => x != null).ToList();

            if (!string.IsNullOrEmpty(item.Conditions))
            {
                RulesManager.RunRules(item.Conditions, sectionViewModel);
            }

            return !sectionViewModel.Visible ? null : sectionViewModel;
        }

        /// <summary>
        ///   Gets the field view model.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="formViewModel">The form view model.</param>
        /// <returns></returns>
        [CanBeNull]
        protected FieldViewModel GetFieldViewModel([NotNull] IFieldItem item, FormViewModel formViewModel)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(formViewModel, "formViewModel");

            var fieldType = item.MVCClass;
            if (string.IsNullOrEmpty(fieldType))
            {
                return new FieldViewModel
                {
                    Item = item.InnerItem
                };
            }

            var type = Type.GetType(fieldType);
            if (type == null)
            {
                return new FieldViewModel
                {
                    Item = item.InnerItem
                };
            }

            var fieldInstance = Activator.CreateInstance(type);
            var fieldViewModel = fieldInstance as FieldViewModel;

            if (fieldViewModel == null)
            {
                Log.Warn(string.Format("[WFFM]Unable to create instance of type {0}", fieldType), this);

                return null;
            }

            fieldViewModel.Title = item.Title ?? string.Empty;
            fieldViewModel.Visible = true;
            if (fieldViewModel is IHasIsRequired)
            {
                ((IHasIsRequired)fieldViewModel).IsRequired = item.IsRequired;
            }
            fieldViewModel.ShowTitle = true;
            fieldViewModel.Item = item.InnerItem;
            fieldViewModel.FormId = formViewModel.Item.ID.ToString();
            fieldViewModel.FormType = formViewModel.FormType;
            fieldViewModel.FieldItemId = item.ID.ToString();
            fieldViewModel.LeftColumnStyle = formViewModel.LeftColumnStyle;
            fieldViewModel.RightColumnStyle = formViewModel.RightColumnStyle;
            fieldViewModel.ShowInformation = true;

            var parameters = item.ParametersDictionary;
            parameters.AddRange(item.LocalizedParametersDictionary);
            fieldViewModel.Parameters = parameters;

            ReflectionUtil.SetXmlProperties(fieldInstance, item.ParametersDictionary);
            ReflectionUtil.SetXmlProperties(fieldInstance, item.LocalizedParametersDictionary);

            fieldViewModel.Parameters.AddRange(item.MvcValidationMessages);

            if (!string.IsNullOrEmpty(item.Conditions))
            {
                RulesManager.RunRules(item.Conditions, fieldViewModel);
            }

            if (!fieldViewModel.Visible)
            {
                return null;
            }

            fieldViewModel.Initialize();

            if (formViewModel.ReadQueryString)
            {

                if (formViewModel.QueryParameters != null && !string.IsNullOrEmpty(formViewModel.QueryParameters[fieldViewModel.Title]))
                {
                    var method = fieldViewModel.GetType().GetMethod("SetValueFromQuery");

                    if (method != null)
                    {
                        method.Invoke(fieldViewModel, new object[]
                        {
              formViewModel.QueryParameters[fieldViewModel.Title]
                        });
                    }
                }
            }
            return fieldViewModel;
        }
    }
}