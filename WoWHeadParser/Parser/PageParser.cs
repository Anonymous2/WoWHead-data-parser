﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using WoWHeadParser.Page;
using WoWHeadParser.Properties;

namespace WoWHeadParser.Parser
{
    public class PageParser
    {
        private Locale _locale;

        public Locale Locale
        {
            get { return _locale > Locale.Old ? _locale : Locale.English; }
            set { _locale = value; }
        }

        public int Flags { get; private set; }

        public PageParser(Locale locale, int flags)
        {
            Flags = flags;
            Locale = locale;
        }

        #region Virtual

        public virtual string PreParse()
        {
            return string.Empty;
        }

        public virtual PageItem Parse(string page, uint id)
        {
            return new PageItem(id, page);
        }

        public virtual int MaxCount { get { return 0; } }

        public virtual string Address { get { return string.Empty; } }

        #endregion

        public List<PageItem> Items = new List<PageItem>(2048);

        public bool TryParse(string page, uint id)
        {
            try
            {
                PageItem item = Parse(page, id);
                Items.Add(item);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while parsing: Parser: {0}, Item: {1} - {2}", GetType().Name, id, e);
                return false;
            }
        }

        public void Sort()
        {
            SortOrder sortOrder = (SortOrder)Settings.Default.SortOrder;
            if (sortOrder > SortOrder.None)
                Items.Sort(new PageItemComparer(sortOrder));
        }

        public override string ToString()
        {
            StringBuilder content = new StringBuilder(Items.Count * 1024);

            string preParse = PreParse().TrimStart();
            if (!string.IsNullOrEmpty(preParse))
                content.Append(preParse);

            Items.ForEach(x => content.Append(x.ToString()));
            return content.ToString();
        }

        #region Locales

        private Dictionary<Locale, string> _locales = new Dictionary<Locale, string>
        {
            {Locale.Russia, "loc8"},
            {Locale.Germany, "loc3"},
            {Locale.France, "loc2"},
            {Locale.Spain, "loc6"},
        };

        public bool HasLocales { get { return _locale > Locale.English; } }

        public string LocalePosfix { get { return _locales[Locale]; } }

        #endregion
    }
}