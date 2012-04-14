﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using Orchard.DisplayManagement;
using Orchard.DisplayManagement.Descriptors;
using Orchard.DisplayManagement.Descriptors.ResourceBindingStrategy;
using Orchard.Environment;
using Orchard.Localization;
using Orchard.Mvc;
using Orchard.Settings;
using Orchard.UI;
using Orchard.UI.Resources;
using Orchard.UI.Zones;
using Orchard.Utility.Extensions;

// ReSharper disable InconsistentNaming

namespace Orchard.Core.Shapes {
    public class CoreShapes : IShapeTableProvider {
        private readonly Work<WorkContext> _workContext;
        private readonly Work<IResourceManager> _resourceManager;
        private readonly Work<IHttpContextAccessor> _httpContextAccessor;

        public CoreShapes(
            Work<WorkContext> workContext, 
            Work<IResourceManager> resourceManager,
            Work<IHttpContextAccessor> httpContextAccessor
            ) {
            _workContext = workContext;
            _resourceManager = resourceManager;
            _httpContextAccessor = httpContextAccessor;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public void Discover(ShapeTableBuilder builder) {
            // the root page shape named 'Layout' is wrapped with 'Document'
            // and has an automatic zone creating behavior
            builder.Describe("Layout")
                .Configure(descriptor => descriptor.Wrappers.Add("Document"))
                .OnCreating(creating => creating.Behaviors.Add(new ZoneHoldingBehavior(() => creating.New.Zone())))
                .OnCreated(created => {
                    var layout = created.Shape;
                    
                    layout.Head = created.New.DocumentZone(ZoneName: "Head");
                    layout.Body = created.New.DocumentZone(ZoneName: "Body");
                    layout.Tail = created.New.DocumentZone(ZoneName: "Tail");

                    layout.Body.Add(created.New.PlaceChildContent(Source: layout));

                    layout.Content = created.New.Zone();
                    layout.Content.ZoneName = "Content";
                    layout.Content.Add(created.New.PlaceChildContent(Source: layout));

                });

            // 'Zone' shapes are built on the Zone base class
            // They have class="zone zone-{name}"
            // and the template can be specialized with "Zone-{Name}" base file name
            builder.Describe("Zone")
                .OnCreating(creating => creating.BaseType = typeof(Zone))
                .OnDisplaying(displaying => {
                    var zone = displaying.Shape;
                    string zoneName = zone.ZoneName;
                    zone.Classes.Add("zone-" + zoneName.HtmlClassify());
                    zone.Classes.Add("zone");

                    // Zone__[ZoneName] e.g. Zone-SideBar
                    zone.Metadata.Alternates.Add("Zone__" + zoneName);
                });

            builder.Describe("Menu")
                .OnDisplaying(displaying => {
                    var menu = displaying.Shape;
                    string menuName = menu.MenuName;
                    menu.Classes.Add("menu-" + menuName.HtmlClassify());
                    menu.Classes.Add("menu");
                    menu.Metadata.Alternates.Add("Menu__" + menuName);
                });

            builder.Describe("MenuItem")
                .OnDisplaying(displaying => {
                    var menuItem = displaying.Shape;
                    var menu = menuItem.Menu;
                    menuItem.Metadata.Alternates.Add("MenuItem__" + menu.MenuName);
                });

            builder.Describe("LocalMenu")
                .OnDisplaying(displaying => {
                    var menu = displaying.Shape;
                    string menuName = menu.MenuName;
                    menu.Classes.Add("localmenu-" + menuName.HtmlClassify());
                    menu.Classes.Add("localmenu");
                    menu.Metadata.Alternates.Add("LocalMenu__" + menuName);
                });
            
            builder.Describe("LocalMenuItem")
                .OnDisplaying(displaying => {
                    var menuItem = displaying.Shape;
                    var menu = menuItem.Menu;
                    menuItem.Metadata.Alternates.Add("LocalMenuItem__" + menu.MenuName);
                });

            #region Pager alternates
            builder.Describe("Pager")
                .OnDisplaying(displaying => {
                    var pager = displaying.Shape;
                    string pagerId = pager.PagerId;
                    if (!String.IsNullOrWhiteSpace(pagerId))
                        displaying.Shape.Metadata.Alternates.Add("Pager__" + EncodeAlternateElement(pagerId));
                });

            builder.Describe("Pager_Gap")
                .OnDisplaying(displaying => {
                    var pager = displaying.Shape.Pager;
                    string pagerId = pager.PagerId;
                    if (!String.IsNullOrWhiteSpace(pagerId))
                        pager.Metadata.Alternates.Add("Pager_Gap__" + EncodeAlternateElement(pagerId));
                });

            builder.Describe("Pager_First")
                .OnDisplaying(displaying => {
                    var pager = displaying.Shape.Pager;
                    string pagerId = pager.PagerId;
                    if (!String.IsNullOrWhiteSpace(pagerId))
                        displaying.Shape.Metadata.Alternates.Add("Pager_First__" + EncodeAlternateElement(pagerId));
                });

            builder.Describe("Pager_Previous")
                .OnDisplaying(displaying => {
                    var pager = displaying.Shape.Pager;
                    string pagerId = pager.PagerId;
                    if (!String.IsNullOrWhiteSpace(pagerId))
                        displaying.Shape.Metadata.Alternates.Add("Pager_Previous__" + EncodeAlternateElement(pagerId));
                });

            builder.Describe("Pager_Next")
                .OnDisplaying(displaying => {
                    var pager = displaying.Shape.Pager;
                    string pagerId = pager.PagerId;
                    if (!String.IsNullOrWhiteSpace(pagerId))
                        displaying.Shape.Metadata.Alternates.Add("Pager_Next__" + EncodeAlternateElement(pagerId));
                });

            builder.Describe("Pager_Last")
                .OnDisplaying(displaying => {
                    var pager = displaying.Shape.Pager;
                    string pagerId = pager.PagerId;
                    if (!String.IsNullOrWhiteSpace(pagerId))
                        displaying.Shape.Metadata.Alternates.Add("Pager_Last__" + EncodeAlternateElement(pagerId));
                });

            builder.Describe("Pager_CurrentPage")
                .OnDisplaying(displaying => {
                    var pager = displaying.Shape.Pager;
                    string pagerId = pager.PagerId;
                    if (!String.IsNullOrWhiteSpace(pagerId))
                        displaying.Shape.Metadata.Alternates.Add("Pager_CurrentPage__" + EncodeAlternateElement(pagerId));
                });

            builder.Describe("Pager_Links")
                .OnDisplaying(displaying => {
                    var pager = displaying.Shape;
                    string pagerId = pager.PagerId;
                    if (!String.IsNullOrWhiteSpace(pagerId))
                        displaying.Shape.Metadata.Alternates.Add("Pager_Links__" + EncodeAlternateElement(pagerId));
                });

            #endregion

            // 'List' shapes start with several empty collections
            builder.Describe("List")
                .OnCreated(created => {
                    var list = created.Shape;
                    list.Tag = "ul";
                    list.ItemClasses = new List<string>();
                    list.ItemAttributes = new Dictionary<string, string>();
                });

            builder.Describe("Style")
                .OnDisplaying(displaying => {
                    var resource = displaying.Shape;
                    string url = resource.Url;
                    string fileName = StylesheetBindingStrategy.GetAlternateShapeNameFromFileName(url);
                    if (!string.IsNullOrEmpty(fileName)) {
                        resource.Metadata.Alternates.Add("Style__" + fileName);
                    }
                });

            builder.Describe("Resource")
                .OnDisplaying(displaying => {
                    var resource = displaying.Shape;
                    string url = resource.Url;
                    string fileName = StylesheetBindingStrategy.GetAlternateShapeNameFromFileName(url);
                    if (!string.IsNullOrEmpty(fileName)) {
                        resource.Metadata.Alternates.Add("Resource__" + fileName);
                    }
                });
        }


        static TagBuilder GetTagBuilder(string tagName, string id, IEnumerable<string> classes, IDictionary<string, string> attributes) {
            var tagBuilder = new TagBuilder(tagName);
            tagBuilder.MergeAttributes(attributes, false);
            foreach (var cssClass in classes ?? Enumerable.Empty<string>())
                tagBuilder.AddCssClass(cssClass);
            if (!string.IsNullOrWhiteSpace(id))
                tagBuilder.GenerateId(id);
            return tagBuilder;
        }

        [Shape]
        public void Zone(dynamic Display, dynamic Shape, TextWriter Output) {
            string id = Shape.Id;
            IEnumerable<string> classes = Shape.Classes;
            IDictionary<string, string> attributes = Shape.Attributes;
            var zoneWrapper = GetTagBuilder("div", id, classes, attributes);
            Output.Write(zoneWrapper.ToString(TagRenderMode.StartTag));
            foreach (var item in ordered_hack(Shape))
                Output.Write(Display(item));
            Output.Write(zoneWrapper.ToString(TagRenderMode.EndTag));
        }

        [Shape]
        public void ContentZone(dynamic Display, dynamic Shape, TextWriter Output) {
            foreach (var item in ordered_hack(Shape))
                Output.Write(Display(item));
        }

        [Shape]
        public void DocumentZone(dynamic Display, dynamic Shape, TextWriter Output) {
            foreach (var item in ordered_hack(Shape))
                Output.Write(Display(item));
        }

        #region ordered_hack

        private static IEnumerable<dynamic> ordered_hack(dynamic shape) {
            IEnumerable<dynamic> unordered = shape;
            if (unordered == null || unordered.Count() < 2)
                return shape;

            var i = 1;
            var progress = 1;
            var flatPositionComparer = new FlatPositionComparer();
            var ordering = unordered.Select(item => {
                var position = (item == null || item.GetType().GetProperty("Metadata") == null || item.Metadata.GetType().GetProperty("Position") == null)
                                   ? null
                                   : item.Metadata.Position;
                return new { item, position };
            }).ToList();

            // since this isn't sticking around (hence, the "hack" in the name), throwing (in) a gnome 
            while (i < ordering.Count()) {
                if (flatPositionComparer.Compare(ordering[i].position, ordering[i - 1].position) > -1) {
                    if (i == progress)
                        progress = ++i;
                    else
                        i = progress;
                }
                else {
                    var higherThanItShouldBe = ordering[i];
                    ordering[i] = ordering[i - 1];
                    ordering[i - 1] = higherThanItShouldBe;
                    if (i > 1)
                        --i;
                }
            }

            return ordering.Select(ordered => ordered.item).ToList();
        }

        #endregion

        [Shape]
        public void HeadScripts(dynamic Display, TextWriter Output) {
            WriteResources(Display, Output, "script", ResourceLocation.Head, null);
            WriteLiteralScripts(Output, _resourceManager.Value.GetRegisteredHeadScripts());
        }

        [Shape]
        public void FootScripts(dynamic Display, TextWriter Output) {
            WriteResources(Display, Output, "script", null, ResourceLocation.Head);
            WriteLiteralScripts(Output, _resourceManager.Value.GetRegisteredFootScripts());
        }

        [Shape]
        public void Metas(TextWriter Output) {
            foreach (var meta in _resourceManager.Value.GetRegisteredMetas() ) {
                Output.WriteLine(meta.GetTag());
            }
        }

        [Shape]
        public void HeadLinks(TextWriter Output) {
            foreach (var link in _resourceManager.Value.GetRegisteredLinks() ) {
                Output.WriteLine(link.GetTag());
            }
        }

        [Shape]
        public void StylesheetLinks(dynamic Display, TextWriter Output) {
            WriteResources(Display, Output, "stylesheet", null, null);
        }

        [Shape]
        public void Style(dynamic Display, HtmlHelper Html, TextWriter Output, ResourceDefinition Resource, string Url, string Condition, Dictionary<string, string> TagAttributes) {
            // do not write to Output directly as Styles are rendered in Zones
            ResourceManager.WriteResource(Html.ViewContext.Writer, Resource, Url, Condition, TagAttributes);
        }

        [Shape]
        public void Resource(TextWriter Output, ResourceDefinition Resource, string Url, string Condition, Dictionary<string, string> TagAttributes) {
            ResourceManager.WriteResource(Output, Resource, Url, Condition, TagAttributes);
        }

        private static void WriteLiteralScripts(TextWriter output, IEnumerable<string> scripts) {
            if (scripts == null) {
                return;
            }
            foreach (string script in scripts) {
                output.WriteLine(script);
            }
        }

        private void WriteResources(dynamic Display, TextWriter Output, string resourceType, ResourceLocation? includeLocation, ResourceLocation? excludeLocation) {
            bool debugMode;
            var site = _workContext.Value.CurrentSite;
            switch (site.ResourceDebugMode) {
                case ResourceDebugMode.Enabled:
                    debugMode = true;
                    break;
                case ResourceDebugMode.Disabled:
                    debugMode = false;
                    break;
                default:
                    Debug.Assert(site.ResourceDebugMode == ResourceDebugMode.FromAppSetting, "Unknown ResourceDebugMode value.");
                    debugMode = _httpContextAccessor.Value.Current().IsDebuggingEnabled;
                    break;
            }
            var defaultSettings = new RequireSettings {
                DebugMode = debugMode,
                Culture = CultureInfo.CurrentUICulture.Name,
            };
            var requiredResources = _resourceManager.Value.BuildRequiredResources(resourceType);
            var appPath = _httpContextAccessor.Value.Current().Request.ApplicationPath;
            foreach (var context in requiredResources.Where(r =>
                (includeLocation.HasValue ? r.Settings.Location == includeLocation.Value : true) &&
                (excludeLocation.HasValue ? r.Settings.Location != excludeLocation.Value : true))) {

                var path = context.GetResourceUrl(defaultSettings, appPath);
                var condition = context.Settings.Condition;
                var attributes = context.Settings.HasAttributes ? context.Settings.Attributes : null;
                IHtmlString result;
                if (resourceType == "stylesheet") {
                    result = Display.Style(Url: path, Condition: condition, Resource: context.Resource, TagAttributes: attributes);
                }
                else {
                    result = Display.Resource(Url: path, Condition: condition, Resource: context.Resource, TagAttributes: attributes);
                }
                Output.Write(result);
            }
        }

        [Shape]
        public IHtmlString Pager_Links(dynamic Shape, dynamic Display,
            HtmlHelper Html,
            int Page,
            int PageSize,
            double TotalItemCount,
            int? Quantity,
            object FirstText,
            object PreviousText,
            object NextText,
            object LastText,
            object GapText,
            string PagerId
            // parameter omitted to workaround an issue where a NullRef is thrown
            // when an anonymous object is bound to an object shape parameter
            /*object RouteValues*/) {

            var currentPage = Page;
            if (currentPage < 1)
                currentPage = 1;

            var pageSize = PageSize;
            if (pageSize < 1)
                pageSize = _workContext.Value.CurrentSite.PageSize;

            var numberOfPagesToShow = Quantity ?? 0;
            if (Quantity == null || Quantity < 0)
                numberOfPagesToShow = 7;
    
            var totalPageCount = Math.Ceiling(TotalItemCount / pageSize);

            var firstText = FirstText ?? T("<<");
            var previousText = PreviousText ?? T("<");
            var nextText = NextText ?? T(">");
            var lastText = LastText ?? T(">>");
            var gapText = GapText ?? T("...");

            // workaround: get it from the shape instead of parameter
            var RouteValues = (object)Shape.RouteValues;

            var routeData = new RouteValueDictionary(Html.ViewContext.RouteData.Values);
            var queryString = _workContext.Value.HttpContext.Request.QueryString;
            if (queryString != null) {
                foreach (var key in from string key in queryString.Keys where key != null && !routeData.ContainsKey(key) let value = queryString[key] select key) {
                    routeData[key] = queryString[key];
                }
            }
    
            if (Shape.RouteData != null) {
                var shapeRouteData = Shape.RouteData is RouteValueDictionary ? (RouteValueDictionary) RouteValues : new RouteValueDictionary(RouteValues);
                foreach (var rd in shapeRouteData) {
                    shapeRouteData[rd.Key] = rd.Value;
                }
            }

            //if (routeData.ContainsKey("id"))
            //    routeData.Remove("id");

            // HACK: MVC 3 is adding a specific value in System.Web.Mvc.Html.ChildActionExtensions.ActionHelper
            // when a content item is set as home page, it is rendered by using Html.RenderAction, and the routeData is altered
            // This code removes this extra route value
            var removedKeys = routeData.Keys.Where(key => routeData[key] is DictionaryValueProvider<object>).ToList();
            foreach (var key in removedKeys) {
                routeData.Remove(key);
            }

            var firstPage = Math.Max(1, Page - (numberOfPagesToShow / 2));
            var lastPage = Math.Min(totalPageCount, Page + (numberOfPagesToShow / 2));

            var pageKey = String.IsNullOrEmpty(PagerId) ? "page" : PagerId;

            Shape.Classes.Add("pager");
            Shape.Metadata.Alternates.Clear();
            Shape.Metadata.Type = "List";

            // first and previous pages
            if (Page > 1) {
                if (routeData.ContainsKey(pageKey)) {
                    routeData.Remove(pageKey); // to keep from having "page=1" in the query string
                }
                // first
                Shape.Add(Display.Pager_First(Value: firstText, RouteValues: routeData, Pager: Shape));

                // previous
                if (currentPage > 2) { // also to keep from having "page=1" in the query string
                    routeData[pageKey] = currentPage - 1;
                }
                Shape.Add(Display.Pager_Previous(Value: previousText, RouteValues: routeData, Pager: Shape));
            }

            // gap at the beginning of the pager
            if (firstPage > 1 && numberOfPagesToShow > 0) {
                Shape.Add(Display.Pager_Gap(Value: gapText, Pager: Shape));
            }

            // page numbers
            if (numberOfPagesToShow > 0) {
                for (var p = firstPage; p <= lastPage; p++) {
                    if (p == currentPage) {
                        Shape.Add(Display.Pager_CurrentPage(Value: p, RouteValues: routeData, Pager: Shape));
                    }
                    else {
                        if (p == 1)
                            routeData.Remove(pageKey);
                        else
                            routeData[pageKey] = p;
                        Shape.Add(Display.Pager_Link(Value: p, RouteValues: routeData, Pager: Shape));
                    }
                }
            }

            // gap at the end of the pager
            if (lastPage < totalPageCount && numberOfPagesToShow > 0) {
                Shape.Add(Display.Pager_Gap(Value: gapText, Pager: Shape));
            }
    
            // next and last pages
            if (Page < totalPageCount) {
                // next
                routeData[pageKey] = Page + 1;
                Shape.Add(Display.Pager_Next(Value: nextText, RouteValues: routeData, Pager: Shape));
                // last
                routeData[pageKey] = totalPageCount;
                Shape.Add(Display.Pager_Last(Value: lastText, RouteValues: routeData, Pager: Shape));
            }

            return Display(Shape);
        }

        [Shape]
        public IHtmlString Pager(dynamic Shape, dynamic Display) {
            Shape.Metadata.Alternates.Clear(); 
            Shape.Metadata.Type = "Pager_Links";
            return Display(Shape);
        }

        [Shape]
        public IHtmlString Pager_First(dynamic Shape, dynamic Display) {
            Shape.Metadata.Alternates.Clear(); 
            Shape.Metadata.Type = "Pager_Link";
            return Display(Shape);
        }
        
        [Shape]
        public IHtmlString Pager_Previous(dynamic Shape, dynamic Display) {
            Shape.Metadata.Alternates.Clear(); 
            Shape.Metadata.Type = "Pager_Link";
            return Display(Shape);
        }
        
        [Shape]
        public IHtmlString Pager_CurrentPage(HtmlHelper Html, dynamic Display, object Value) {
            var tagBuilder = new TagBuilder("span");
            tagBuilder.InnerHtml = Html.Encode(Value is string ? (string)Value : Display(Value));
            
            return MvcHtmlString.Create(tagBuilder.ToString());
        }
        
        [Shape]
        public IHtmlString Pager_Next(dynamic Shape, dynamic Display) {
            Shape.Metadata.Alternates.Clear(); 
            Shape.Metadata.Type = "Pager_Link";
            return Display(Shape);
        }
        
        [Shape]
        public IHtmlString Pager_Last(dynamic Shape, dynamic Display) {
            Shape.Metadata.Alternates.Clear(); 
            Shape.Metadata.Type = "Pager_Link";
            return Display(Shape);
        }
        
        [Shape]
        public IHtmlString Pager_Link(dynamic Shape, dynamic Display) {
            Shape.Metadata.Alternates.Clear(); 
            Shape.Metadata.Type = "ActionLink";
            return Display(Shape);
        }

        [Shape]
        public IHtmlString ActionLink(HtmlHelper Html, dynamic Shape, dynamic Display, object Value) {
            var RouteValues = (object)Shape.RouteValues;
            RouteValueDictionary rvd;
            if (RouteValues == null) {
                rvd = new RouteValueDictionary();
            }
            else {
                rvd = RouteValues is RouteValueDictionary ? (RouteValueDictionary)RouteValues : new RouteValueDictionary(RouteValues);
            }
            
            string value = Html.Encode(Value is string ? (string)Value : Display(Value));
            return @Html.ActionLink(value, (string)rvd["action"], (string)rvd["controller"], rvd, null);
        }

        [Shape]
        public IHtmlString Pager_Gap(HtmlHelper Html, dynamic Display, object Value) {
            var tagBuilder = new TagBuilder("span");
            tagBuilder.InnerHtml = Html.Encode(Value is string ? (string)Value : Display(Value));

            return MvcHtmlString.Create(tagBuilder.ToString());
        }

        [Shape]
        public void List(
            dynamic Display,
            TextWriter Output,
            IEnumerable<dynamic> Items,
            string Tag,
            string Id,
            IEnumerable<string> Classes,
            IDictionary<string, string> Attributes,
            IEnumerable<string> ItemClasses,
            IDictionary<string, string> ItemAttributes) {

            if (Items == null)
                return;

            var itemDisplayOutputs = Items.Select(item => Display(item)).Where(output => !string.IsNullOrWhiteSpace(output.ToHtmlString())).ToList();
            var count = itemDisplayOutputs.Count();
            if (count < 1)
                return;

            var listTagName = string.IsNullOrEmpty(Tag) ? "ul" : Tag;
            const string itemTagName = "li";

            var listTag = GetTagBuilder(listTagName, Id, Classes, Attributes);
            Output.Write(listTag.ToString(TagRenderMode.StartTag));

            var index = 0;
            foreach (var itemDisplayOutput in itemDisplayOutputs) {
                var itemTag = GetTagBuilder(itemTagName, null, ItemClasses, ItemAttributes);
                if (index == 0)
                    itemTag.AddCssClass("first");
                if (index == count - 1)
                    itemTag.AddCssClass("last");
                Output.Write(itemTag.ToString(TagRenderMode.StartTag));
                Output.Write(itemDisplayOutput);
                Output.Write(itemTag.ToString(TagRenderMode.EndTag));
                ++index;
            }

            Output.Write(listTag.ToString(TagRenderMode.EndTag));
        }

        [Shape]
        public IHtmlString PlaceChildContent(dynamic Source) {
            return Source.Metadata.ChildContent;
        }

        [Shape]
        public void Partial(HtmlHelper Html, TextWriter Output, string TemplateName, object Model, string Prefix) {
            RenderInternal(Html, Output, TemplateName, Model, Prefix);
        }

        [Shape]
        public void DisplayTemplate(HtmlHelper Html, TextWriter Output, string TemplateName, object Model, string Prefix) {
            RenderInternal(Html, Output, "DisplayTemplates/" + TemplateName, Model, Prefix);
        }

        [Shape]
        public void EditorTemplate(HtmlHelper Html, TextWriter Output, string TemplateName, object Model, string Prefix) {
            RenderInternal(Html, Output, "EditorTemplates/" + TemplateName, Model, Prefix);
        }

        static void RenderInternal(HtmlHelper Html, TextWriter Output, string TemplateName, object Model, string Prefix) {
            var adjustedViewData = new ViewDataDictionary(Html.ViewDataContainer.ViewData) {
                Model = DetermineModel(Html, Model),
                TemplateInfo = new TemplateInfo {
                    HtmlFieldPrefix = DeterminePrefix(Html, Prefix)
                }
            };
            var adjustedViewContext = new ViewContext(Html.ViewContext, Html.ViewContext.View, adjustedViewData, Html.ViewContext.TempData, Output);
            var adjustedHtml = new HtmlHelper(adjustedViewContext, new ViewDataContainer(adjustedViewData));
            adjustedHtml.RenderPartial(TemplateName);
        }

        static object DetermineModel(HtmlHelper Html, object Model) {
            bool isNull = ((dynamic)Model) == null;
            return isNull ? Html.ViewData.Model : Model;
        }

        static string DeterminePrefix(HtmlHelper Html, string Prefix) {
            var actualPrefix = string.IsNullOrEmpty(Prefix)
                                   ? Html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix
                                   : Html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(Prefix);
            return actualPrefix;
        }

        private class ViewDataContainer : IViewDataContainer {
            public ViewDataContainer(ViewDataDictionary viewData) { ViewData = viewData; }
            public ViewDataDictionary ViewData { get; set; }
        }

        /// <summary>
        /// Encodes dashed and dots so that they don't conflict in filenames 
        /// </summary>
        /// <param name="alternateElement"></param>
        /// <returns></returns>
        private string EncodeAlternateElement(string alternateElement) {
            return alternateElement.Replace("-", "__").Replace(".", "_");
        }
    }
}
