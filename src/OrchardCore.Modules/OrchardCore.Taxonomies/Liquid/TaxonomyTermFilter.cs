using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.Liquid;
using OrchardCore.Taxonomies.Fields;

namespace OrchardCore.Taxonomies.Liquid
{
    public class TaxonomyTermsFilter : ILiquidFilter
    {
        public async Task<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext ctx)
        {
            if (!ctx.AmbientValues.TryGetValue("Services", out var services))
            {
                throw new ArgumentException("Services missing while invoking 'taxonomy_terms'");
            }

            var contentManager = ((IServiceProvider)services).GetRequiredService<IContentManager>();

            string taxonomyContentItemId = null;
            string[] termContentItemIds = null;

            if (input.Type == FluidValues.Object && input.ToObjectValue() is TaxonomyField field)
            {
                taxonomyContentItemId = field.TaxonomyContentItemId;
                termContentItemIds = field.TermContentItemIds;
            }
            if (input.Type == FluidValues.Object
                && input.ToObjectValue() is JObject jobj
                && jobj.ContainsKey(nameof(TaxonomyField.TermContentItemIds))
                && jobj.ContainsKey(nameof(TaxonomyField.TaxonomyContentItemId)))
            {
                taxonomyContentItemId = jobj["TaxonomyContentItemId"].Value<string>();
                termContentItemIds = ((JArray)jobj["TermContentItemIds"]).Values<string>().ToArray();
            }
            else if (input.Type == FluidValues.Array)
            {
                taxonomyContentItemId = arguments.At(0).ToStringValue();
                termContentItemIds = input.Enumerate().Select(x => x.ToStringValue()).ToArray();
            }
            else
            {
                return NilValue.Instance;
            }

            var taxonomy = await contentManager.GetAsync(taxonomyContentItemId);

            if (taxonomy == null)
            {
                return null;
            }

            var terms = new List<ContentItem>();

            foreach (var termContentItemId in termContentItemIds)
            {
                var term = TaxonomyOrchardHelperExtensions.FindTerm(taxonomy.Content.TaxonomyPart.Terms as JArray, termContentItemId);
                terms.Add(term);
            }

            return FluidValue.Create(terms);
        }
    }
}
