using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Common;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.LanguageFallback;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Globalization;
using System.Reflection;

namespace Sitecore.Support.Globalization
{
    public class ItemEventHandler
    {
        private void OnItemSavedRemote(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull((object)args, "args");
            using (new LanguageFallbackItemSwitcher(new bool?(true)))
            {
                Sitecore.Globalization.ItemEventHandler itemEventHandler = new Sitecore.Globalization.ItemEventHandler();
                string str = (string)itemEventHandler.GetType().InvokeMember("OnItemSavedRemote", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, (Binder)null, (object)itemEventHandler, new object[2]
                {
          sender,
          (object) args
                });
            }
        }

        private void OnItemSaved(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull((object)args, "args");
            Item parameter = Event.ExtractParameter(args, 0) as Item;
            if (parameter == null)
                return;
            using (new LanguageFallbackItemSwitcher(new bool?(true)))
            {
                if (parameter.TemplateID == TemplateIDs.DictionaryEntry)
                    this.UpdateCacheEntries(parameter[FieldIDs.DictionaryKey], parameter[FieldIDs.DictionaryKey], parameter);
                Sitecore.Globalization.ItemEventHandler itemEventHandler = new Sitecore.Globalization.ItemEventHandler();
                string str = (string)itemEventHandler.GetType().InvokeMember("OnItemSaved", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, (Binder)null, (object)itemEventHandler, new object[2]
                {
          sender,
          (object) args
                });
            }
        }

        internal void UpdateCacheEntries(string oldDictionaryKey, string newDictionaryKey, Item item)
        {
            Assert.ArgumentNotNull((object)item, "item");
            Assert.ArgumentNotNull((object)oldDictionaryKey, "oldDictionaryKey");
            Assert.ArgumentNotNull((object)newDictionaryKey, "newDictionaryKey");
            DictionaryDomain dictionaryDomainForEntry = this.FindDictionaryDomainForEntry(item, item.Parent);
            if (dictionaryDomainForEntry == null)
            {
                Log.Error(string.Format("Cannot find dictionary domain for the dictionaty entry '{0}'('{1}').", (object)item.Paths.FullPath, (object)item.ID), (object)this);
            }
            else
            {
                if (!string.IsNullOrEmpty(oldDictionaryKey))
                    this.RemoveEntriesFromCache(oldDictionaryKey, dictionaryDomainForEntry);
                this.CachePhrases(newDictionaryKey, item, dictionaryDomainForEntry);
                this.UpdateCacheEntries(oldDictionaryKey, newDictionaryKey, item, DictionaryDomain.GetDefaultDomain(Database.GetDatabase("master")));
                Translate translate = new Translate();
                string str = (string)translate.GetType().InvokeMember("Save", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, (Binder)null, (object)translate, new object[0]);
            }
        }

        private DictionaryDomain FindDictionaryDomainForEntry(Item entry, Item parent)
        {
            Assert.IsNotNull((object)entry, "dictionary entry");
            Item obj = parent;
            while (obj != null && obj.TemplateID != TemplateIDs.DictionaryDomain && obj.ID != ItemIDs.Dictionary)
                obj = obj.Parent;
            DictionaryDomain dictionaryDomain;
            if (obj == null)
            {
                dictionaryDomain = (DictionaryDomain)null;
            }
            else
            {
                DictionaryDomain domain;
                DictionaryDomain.TryParse(obj.ID.ToString(), obj.Database, out domain);
                dictionaryDomain = domain;
            }
            return dictionaryDomain;
        }

        private void RemoveEntriesFromCache(string dictionaryKey, DictionaryDomain domain)
        {
            Translate.RemoveKeyFromCache(dictionaryKey, Language.Invariant, domain);
        }

        private void CachePhrases(string dictionaryKey, Item item, DictionaryDomain domain)
        {
            bool? currentValue = Switcher<bool?, LanguageFallbackItemSwitcher>.CurrentValue;
            foreach (Language cachedLanguage in Translate.GetCachedLanguages(domain))
            {
                Item obj = item.Database.GetItem(item.ID, cachedLanguage);
                if (obj != null)
                {
                    obj.Fields[FieldIDs.DictionaryPhrase].GetValue(true, true);
                    Translate.CachePhrase(dictionaryKey, obj[FieldIDs.DictionaryPhrase], cachedLanguage, domain);
                }
            }
        }

        internal void UpdateCacheEntries(string oldDictionaryKey, string newDictionaryKey, Item item, DictionaryDomain domain)
        {
            if (domain == null)
                return;
            if (!string.IsNullOrEmpty(oldDictionaryKey))
                this.RemoveEntriesFromCache(oldDictionaryKey, domain);
            this.CachePhrases(newDictionaryKey, item, domain);
            item.RuntimeSettings.ForceModified = true;
            Translate translate = new Translate();
            string str = (string)translate.GetType().InvokeMember("Save", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, (Binder)null, (object)translate, new object[0]);
        }
    }
}