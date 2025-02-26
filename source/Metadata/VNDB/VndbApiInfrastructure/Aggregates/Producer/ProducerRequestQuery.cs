﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VndbApiDomain.ProducerAggregate;
using VndbApiInfrastructure.SharedKernel;
using VndbApiInfrastructure.SharedKernel.Filters;
using VndbApiInfrastructure.SharedKernel.Requests;

namespace VndbApiInfrastructure.ProducerAggregate
{
    public class ProducerRequestFields : RequestFieldAbstractBase, IRequestFields
    {
        public ProducerRequestFieldsFlags Flags =
            ProducerRequestFieldsFlags.Id | ProducerRequestFieldsFlags.Name | ProducerRequestFieldsFlags.Type;

        public void EnableAllFlags(bool enableSubfields)
        {
            EnumUtilities.SetAllEnumFlags(ref Flags);
        }

        public void DisableAllFlags(bool disableSubfields)
        {
            Flags = default;
        }

        public override List<string> GetFlagsStringRepresentations(params string[] prefixParts)
        {
            var prefix = GetFullPrefixString(prefixParts);
            return EnumUtilities.GetStringRepresentations(Flags, prefix);
        }
    }

    public class ProducerRequestQuery : RequestQueryBase
    {
        [JsonIgnore]
        public ProducerRequestFields Fields = new ProducerRequestFields();
        [JsonIgnore]
        public ProducerRequestSortEnum Sort = ProducerRequestSortEnum.SearchRank;

        public ProducerRequestQuery(SimpleFilterBase<Producer> filter) : base(filter)
        {

        }

        public ProducerRequestQuery(ComplexFilterBase<Producer> filter) : base(filter)
        {

        }

        protected override List<string> GetEnabledFields()
        {
            return Fields.GetFlagsStringRepresentations();
        }

        protected override string GetSortString()
        {
            if (Filters is SimpleFilterBase<Producer> simpleFilter)
            {
                if (Sort == ProducerRequestSortEnum.SearchRank)
                {
                    if (simpleFilter.Name != ProducerFilterFactory.Search.FilterName)
                    {
                        return null;
                    }
                }
            }
            else if (Filters is ComplexFilterBase<Producer> complexFilter)
            {
                var simplePredicates = complexFilter.Filters.OfType<SimpleFilterBase<Producer>>();
                if (Sort == ProducerRequestSortEnum.SearchRank)
                {
                    var searchPredicatesCount = simplePredicates.Count(x => x.Name == ProducerFilterFactory.Search.FilterName);
                    if (searchPredicatesCount != 1)
                    {
                        return null;
                    }
                }
            }

            return EnumUtilities.GetEnumStringRepresentation(Sort);
        }
    }
}