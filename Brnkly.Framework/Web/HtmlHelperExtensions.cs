using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Brnkly.Framework.Web
{
    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString DurationDropDownListFor<TModel>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, int>> expression,
            int startSeconds,
            int stepSeconds,
            int maxSeconds,
            object htmlAttributes)
        {
            if (maxSeconds <= startSeconds)
            {
                throw new InvalidOperationException("maxSeconds must exceed startSeconds");
            }

            if (stepSeconds == 0)
            {
                throw new InvalidOperationException("stepSeconds must be greater than 0");
            }

            if (maxSeconds >= 3600)
            {
                throw new InvalidOperationException("DurationDropDownListFor only supports up to 1 hour");
            }

            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);

            var selectItems = new List<SelectListItem>();

            int currentSeconds = startSeconds;
            while (currentSeconds <= maxSeconds)
            {
                var displayBuilder = new StringBuilder();
                if (currentSeconds == 0)
                {
                    displayBuilder.Append("OFF");
                }
                else
                {
                    int minutes = currentSeconds / 60;
                    if (minutes > 0)
                    {
                        displayBuilder.AppendFormat("{0} minutes ", minutes);
                    }

                    int seconds = currentSeconds % 60;
                    if (seconds > 0)
                    {
                        displayBuilder.AppendFormat("{0} seconds ", seconds);
                    }
                }

                selectItems.Add(new SelectListItem()
                {
                    Value = currentSeconds.ToString(),
                    Text = displayBuilder.ToString(),
                    Selected = currentSeconds.Equals(metadata.Model),
                });

                currentSeconds += stepSeconds;
            }

            return htmlHelper.DropDownListFor(
                expression,
                selectItems,
                htmlAttributes);
        }

        public static IHtmlString PropertyNameFor<T, TValue>(
             this HtmlHelper<T> htmlHelper,
             Expression<Func<T, TValue>> propertySelector)
        {
            var propertyName = ExpressionHelper.GetExpressionText(propertySelector);

            return MvcHtmlString.Create(propertyName);
        }

        public static IHtmlString DataAttrFor<T, TValue>(
            this HtmlHelper<T> htmlHelper,
            Expression<Func<T, TValue>> propertySelector,
            string attributeName = null)
        {
            attributeName = attributeName
                            ?? ExpressionHelper.GetExpressionText(propertySelector);

            var obj = ModelMetadata.FromLambdaExpression(propertySelector, htmlHelper.ViewData)
                .Model;

            return DataAttr(htmlHelper, attributeName, obj);
        }

        public static IHtmlString DataAttr(
            this HtmlHelper htmlHelper,
            string attributeName,
            object obj)
        {
            var serializedValue = obj != null
                                      ? GetSerializedValue(obj)
                                      : null;

            var attribute = new XAttribute("data-" + attributeName, serializedValue ?? string.Empty);

            return MvcHtmlString.Create(attribute.ToString());
        }

        private static object GetSerializedValue(object obj)
        {
            var type = obj.GetType();
            if (type.IsPrimitive || typeof(string).IsAssignableFrom(type))
            {
                return obj;
            }

            return JsonConvert.SerializeObject(obj);
        }

        public static MvcHtmlString EnumDropDownListFor<TModel, TEnum>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TEnum>> expression,
            object htmlAttributes,
            IEnumerable<TEnum> optionsToIgnore = null)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);

            IEnumerable<TEnum> values =
                Enum.GetValues(typeof(TEnum))
                    .Cast<TEnum>()
                    .Except(optionsToIgnore ?? Enumerable.Empty<TEnum>());

            var items = values
                .Select(value =>
                    new SelectListItem
                    {
                        Text = value.ToString(),
                        Value = value.ToString(),
                        Selected = value.Equals(metadata.Model)
                    });

            return htmlHelper.DropDownListFor(
                expression,
                items,
                htmlAttributes);
        }

        public static IHtmlString RichTextEditorFor<TModel>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, string>> propertySelector,
            string iframeSrc,
            object htmlAttributes)
        {
            var hidden = htmlHelper.HiddenFor(propertySelector).ToHtmlString();
            var tagBuilder = new TagBuilder("iframe");

            tagBuilder.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));

            var propertyName = ExpressionHelper.GetExpressionText(propertySelector);
            var javaScriptVariableName = "richTextEditorFor" + propertyName;
            var onLoadJs = string.Format("top.{0}=this.contentWindow.editor", javaScriptVariableName);

            tagBuilder.MergeAttribute("src", iframeSrc);
            tagBuilder.MergeAttribute("data-property-id", propertyName);
            tagBuilder.MergeAttribute("data-js-link", javaScriptVariableName);
            tagBuilder.MergeAttribute("onload", onLoadJs);

            tagBuilder.AddCssClass("rich-text-editor");

            return MvcHtmlString.Create(tagBuilder + hidden);
        }

        public static IHtmlString ContentEditableFor<TModel>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, string>> propertySelector,
            string tagName = "div",
            object htmlAttributes = null)
        {
            var tagBuilder = new TagBuilder(tagName);
            var propertyName = ExpressionHelper.GetExpressionText(propertySelector);

            var propertyValue =
                ModelMetadata.FromLambdaExpression(propertySelector, htmlHelper.ViewData)
                    .Model as string;

            var hidden = htmlHelper.HiddenFor(propertySelector).ToHtmlString();

            tagBuilder.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));

            tagBuilder.MergeAttribute("data-property-id", propertyName);
            tagBuilder.MergeAttribute("contenteditable", "true");
            tagBuilder.AddCssClass("content-editable");
            tagBuilder.AddCssClass("edit-" + propertyName);

            tagBuilder.SetInnerText(propertyValue);

            return MvcHtmlString.Create(tagBuilder + hidden);
        }

        public static IHtmlString AutocompleteFor<TModel>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, string>> targetPropertySelector,
            Expression<Func<TModel, string>> friendlyProperySelector,
            object htmlAttributes = null)
        {
            var friendlyInputBuilder = new TagBuilder("input");
            var targetPropertyName = ExpressionHelper.GetExpressionText(targetPropertySelector);
            var friendlyPropertyName = ExpressionHelper.GetExpressionText(friendlyProperySelector);
            var hidden = htmlHelper.HiddenFor(targetPropertySelector).ToHtmlString();

            var friendlyPropertyValue =
                ModelMetadata.FromLambdaExpression(friendlyProperySelector, htmlHelper.ViewData)
                    .Model as string;

            friendlyInputBuilder.MergeAttributes(
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));

            friendlyInputBuilder.MergeAttribute("name", friendlyPropertyName);
            friendlyInputBuilder.MergeAttribute("type", "text");
            friendlyInputBuilder.MergeAttribute("value", friendlyPropertyValue);
            friendlyInputBuilder.MergeAttribute("data-property-id", targetPropertyName);

            friendlyInputBuilder.AddCssClass("edit-" + targetPropertyName);
            friendlyInputBuilder.AddCssClass("autocomplete");

            return MvcHtmlString.Create(friendlyInputBuilder + hidden);
        }

        public static IHtmlString AutocompleteListFor<TModel>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, IEnumerable<string>>> targetPropertySelector,
            Expression<Func<TModel, IEnumerable<string>>> friendlyProperySelector,
            object htmlAttributes = null)
        {
            var friendlyInputBuilder = new TagBuilder("input");
            var targetPropertyName = ExpressionHelper.GetExpressionText(targetPropertySelector);
            var friendlyPropertyName = ExpressionHelper.GetExpressionText(friendlyProperySelector);

            var friendlyPropertyValue =
                (ModelMetadata.FromLambdaExpression(friendlyProperySelector, htmlHelper.ViewData)
                     .Model as IEnumerable<string>
                 ?? Enumerable.Empty<string>())
                    .ToList();

            var targetPropertyValue =
                (ModelMetadata.FromLambdaExpression(targetPropertySelector, htmlHelper.ViewData)
                     .Model as IEnumerable<string>
                 ?? Enumerable.Empty<string>())
                    .ToList();

            var targetHidden = htmlHelper.Hidden(
                targetPropertyName,
                string.Join(",", targetPropertyValue),
                new { id = targetPropertyName });

            var friendlyHidden = htmlHelper.Hidden(
                friendlyPropertyName,
                string.Join(",", friendlyPropertyValue),
                new { id = friendlyPropertyName });

            friendlyInputBuilder.MergeAttributes(
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));

            //friendlyInputBuilder.MergeAttribute("name", friendlyPropertyName);
            friendlyInputBuilder.MergeAttribute("type", "text");
            friendlyInputBuilder.MergeAttribute("value", null);
            friendlyInputBuilder.MergeAttribute("data-property-id", targetPropertyName);
            friendlyInputBuilder.MergeAttribute("data-friendly-id", friendlyPropertyName);

            friendlyInputBuilder.AddCssClass("edit-" + targetPropertyName);
            friendlyInputBuilder.AddCssClass("autocomplete list");

            var currentResultsList = new TagBuilder("ul");
            currentResultsList.AddCssClass("current-results");
            currentResultsList.AddCssClass("clearfix");

            if (!friendlyPropertyValue.Any())
            {
                currentResultsList.AddCssClass("empty");
            }

            // TODO: make sure list lengths are equal.
            var entryTags = friendlyPropertyValue
                .Zip(targetPropertyValue, (friendlyName, id) => new { id, friendlyName });

            foreach (var entryTag in entryTags)
            {
                var li = new TagBuilder("li");
                li.SetInnerText(entryTag.friendlyName);
                li.MergeAttribute("data-id", entryTag.id);
                currentResultsList.InnerHtml += li.ToString();
            }

            return MvcHtmlString.Create(friendlyInputBuilder.ToString() + currentResultsList + targetHidden + friendlyHidden);
        }

        public static IHtmlString ValidationClassFor<TModel, TValue>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TValue>> propertySelector,
            string classToAdd)
        {
            var modelName = ExpressionHelper.GetExpressionText(propertySelector);

            if (!htmlHelper.ViewData.ModelState.ContainsKey(modelName))
            {
                return null;
            }

            var modelState = htmlHelper.ViewData.ModelState[modelName];

            if (modelState != null
                && modelState.Errors != null
                && modelState.Errors.Any())
            {
                return MvcHtmlString.Create(classToAdd);
            }

            return null;
        }
    }
}