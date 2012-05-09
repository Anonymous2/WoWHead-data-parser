﻿using System;
using System.Collections.Generic;
using WoWHeadParser.Page;

namespace WoWHeadParser.Parser
{
    public class DataParser
    {
        public virtual void Prepare()
        {
        }

        public virtual string PreParse()
        {
            return string.Empty;
        }

        public virtual string Parse(PageItem block)
        {
            return string.Empty;
        }

        public virtual string Address { get { return string.Empty; } }

        public virtual string Name { get { return string.Empty; } }

        public virtual int MaxCount { get { return 0; } }

        public string SafeParser(PageItem item)
        {
            try
            {
                return Parse(item);
            }
            catch (Exception e)
            {
                Console.WriteLine("[{0}]: {1}", item.Id, e.Message);
                return string.Empty;
            }
        }

        #region Locales

        public Locale Locale = Locale.English;

        public Dictionary<Locale, string> Locales = new Dictionary<Locale, string>
        {
            {Locale.Russia, "loc8"},
            {Locale.Germany, "loc3"},
            {Locale.France, "loc2"},
            {Locale.Spain, "loc6"},
        };

        public bool HasLocales { get { return Locale > Locale.English; } }

        #endregion
    }
}