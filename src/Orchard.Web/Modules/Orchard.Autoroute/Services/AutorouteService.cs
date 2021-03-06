﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Orchard.Alias;
using Orchard.Autoroute.Models;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Localization;
using Orchard.Tokens;
using Orchard.Autoroute.Settings;
using Orchard.ContentManagement.MetaData;
using Orchard.Logging;
using Orchard.ContentManagement;

namespace Orchard.Autoroute.Services {
    public class AutorouteService : IAutorouteService {

        private readonly IAliasService _aliasService;
        private readonly ITokenizer _tokenizer;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentManager _contentManager;

        public AutorouteService(
            IAliasService aliasService,
            ITokenizer tokenizer,
            IContentDefinitionManager contentDefinitionManager,
            IContentManager contentManager) {
                _aliasService = aliasService;
                _tokenizer = tokenizer;
                _contentDefinitionManager = contentDefinitionManager;
            _contentManager = contentManager;

            Logger = NullLogger.Instance;
                T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public string GenerateAlias(AutoroutePart part) {

            if (part == null) {
                throw new ArgumentNullException("part");
            }

            var defaultRoutePattern = GetDefaultPattern(part.ContentItem.ContentType);

            var pattern = part.UseCustomPattern ? part.CustomPattern : defaultRoutePattern.Pattern;

            // Convert the pattern and route values via tokens
            var path = _tokenizer.Replace(pattern, BuildTokenContext(part.ContentItem), new ReplaceOptions { Encoding = ReplaceOptions.NoEncode });

            // removing trailing slashes in case the container is empty, and tokens are base on it (e.g. home page)
            while(path.StartsWith("/")) {
                path = path.Substring(1);
            }

            return path;
        }

        public void PublishAlias(AutoroutePart part) {
            var displayRouteValues = _contentManager.GetItemMetadata(part).DisplayRouteValues;
            var aliasSource = "Autoroute:View:" + part.Id;

            _aliasService.Replace(part.DisplayAlias, displayRouteValues, aliasSource);    
        }

        private IDictionary<string, object> BuildTokenContext(IContent item) {
            return new Dictionary<string, object> { { "Content", item } };
        }

        public void RegenerateAlias(string aliasSource) {
            var info = ParseSource(aliasSource);
            if (info == null) return;
            /* TODO:
            if (pattern == null) {
                if (info.Item == null) return;
                pattern = GetPattern(info.Scope, info.Name, info.Item);
                if (pattern == null) return;
            }

            // TODO: Can fire "changed" event with impunity right here
            // Remove old alias
            _aliasService.DeleteBySource(aliasSource);

            if (info.Item!=null) {
            var patternDef = _describe.Value.Descriptors
                .Where(d => d.Scope == pattern.PatternScope && d.PatternName == pattern.PatternName).FirstOrDefault();

                if (patternDef != null)
                    RegisterAlias(aliasSource, pattern.Pattern, patternDef.RouteFactory(info.Item), BuildTokenContext(patternDef.ScopeDescriptor,info.Item));
            }
            */
        }

        ParsedSourceInfo ParseSource(string source) {
            if (string.IsNullOrWhiteSpace(source)) return null;
            var parts = source.Split(':');
            // Must consist of Autoroute:Scope:Name:Id
            if (parts.Length < 3) {
                return null;
            }
            if (parts[0] != "Autoroute") return null;
            var info = new ParsedSourceInfo() {
                Name = parts[1],
                Id = Convert.ToInt32(parts[2])
            };
            return info;
        }

        class ParsedSourceInfo {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public void CreatePattern(string contentType, string name, string pattern, string description, bool makeDefault) {
            var contentDefinition = _contentDefinitionManager.GetTypeDefinition(contentType);

            if (contentDefinition == null) {
                throw new OrchardException(T("Unknown content type: {0}", contentType));
            }

            var settings = contentDefinition.Settings.GetModel<AutorouteSettings>();

            var routePattern = new RoutePattern {
                Description = description,
                Name = name,
                Pattern = pattern
            };

            var patterns = settings.Patterns;
            patterns.Add(routePattern);
            settings.Patterns = patterns;

            // define which pattern is the default
            if (makeDefault || settings.Patterns.Count == 1) {
                settings.DefaultPatternIndex = settings.Patterns.IndexOf(routePattern);
            }

            _contentDefinitionManager.AlterTypeDefinition(contentType, builder => builder.WithPart("AutoroutePart", settings.Build));
        }

        public IEnumerable<RoutePattern> GetPatterns(string contentType) {
            var settings = GetTypePartSettings(contentType).GetModel<AutorouteSettings>();
            return settings.Patterns;
        }

        public RoutePattern GetDefaultPattern(string contentType) {
            var settings = GetTypePartSettings(contentType).GetModel<AutorouteSettings>();
            return settings.Patterns.ElementAt(settings.DefaultPatternIndex);
        }

        public void RemoveAliases(AutoroutePart part) {
            
            // todo: remove all aliases for this part

            _aliasService.Delete(part.DisplayAlias);
        }

        private SettingsDictionary GetTypePartSettings(string contentType) {
            var contentDefinition = _contentDefinitionManager.GetTypeDefinition(contentType);
            
            if (contentDefinition == null) {
                throw new OrchardException(T("Unknown content type: {0}", contentType));
            }
            
            return contentDefinition.Parts.First(x => x.PartDefinition.Name == "AutoroutePart").Settings;
        }

        public string GenerateUniqueSlug(AutoroutePart part, IEnumerable<string> existingPaths) {
            if (existingPaths == null || !existingPaths.Contains(part.Path))
                return part.Path;

            int? version = existingPaths.Select(s => GetSlugVersion(part.Path, s)).OrderBy(i => i).LastOrDefault();

            return version != null
                ? string.Format("{0}-{1}", part.Path, version)
                : part.Path;
        }

        private static int? GetSlugVersion(string path, string potentialConflictingPath) {
            int v;
            string[] slugParts = potentialConflictingPath.Split(new[] { path }, StringSplitOptions.RemoveEmptyEntries);

            if (slugParts.Length == 0)
                return 2;

            return int.TryParse(slugParts[0].TrimStart('-'), out v)
                       ? (int?)++v
                       : null;
        }

        public IEnumerable<AutoroutePart> GetSimilarPaths(string path) {
            return
                _contentManager.Query<AutoroutePart, AutoroutePartRecord>()
                    .Where(part => part.DisplayAlias != null && part.DisplayAlias.StartsWith(path))
                    .List();
        }

        public bool IsPathValid(string slug) {
            return String.IsNullOrWhiteSpace(slug) || Regex.IsMatch(slug, @"^[^:?#\[\]@!$&'()*+,;=\s\""\<\>\\]+$") && !(slug.StartsWith(".") || slug.EndsWith("."));
        }

        public bool ProcessPath(AutoroutePart part) {

            var pathsLikeThis = GetSimilarPaths(part.Path);

            // Don't include *this* part in the list
            // of slugs to consider for conflict detection
            pathsLikeThis = pathsLikeThis.Where(p => p.ContentItem.Id != part.ContentItem.Id).ToList();

            if (pathsLikeThis.Any()) {
                var originalPath = part.Path;
                var newPath = GenerateUniqueSlug(part, pathsLikeThis.Select(p => p.Path));
                part.DisplayAlias = newPath;

                if (originalPath != newPath)
                    return false;
            }

            return true;
        }
    }
}
