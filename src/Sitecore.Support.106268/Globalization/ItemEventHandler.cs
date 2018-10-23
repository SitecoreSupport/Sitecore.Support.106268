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
using Sitecore.Data.Events;

namespace Sitecore.Support.Globalization
{
    public class ItemEventHandler
    {
        private void OnItemSavedRemote(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            ItemSavedRemoteEventArgs itemSavedRemoteEventArgs = args as ItemSavedRemoteEventArgs;
            if (itemSavedRemoteEventArgs != null && itemSavedRemoteEventArgs.Item != null && !(itemSavedRemoteEventArgs.Item.TemplateID != TemplateIDs.DictionaryEntry))
            {
                using (new LanguageFallbackItemSwitcher(new bool?(IsItemFallbackEnabled(itemSavedRemoteEventArgs.Item))))
                {
                    Sitecore.Globalization.ItemEventHandler itemEventHandler = new Sitecore.Globalization.ItemEventHandler();
                    string str = (string)itemEventHandler.GetType().InvokeMember("OnItemSavedRemote", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, (Binder)null, (object)itemEventHandler, new object[2]
                    {
                        sender,
                        (object) args
                    });
                }
            }
        }

        private void OnItemSaved(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull((object)args, "args");
            Item parameter = Event.ExtractParameter(args, 0) as Item;
            if (parameter == null)
                return;
            //using (new LanguageFallbackItemSwitcher(new bool?(true)))
            using (new LanguageFallbackItemSwitcher(new bool?(IsItemFallbackEnabled(parameter))))
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

        private void OnVersionRemovedRemote(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            VersionRemovedRemoteEventArgs itemSavedRemoteEventArgs = args as VersionRemovedRemoteEventArgs;
            if (itemSavedRemoteEventArgs != null && itemSavedRemoteEventArgs.Item != null && !(itemSavedRemoteEventArgs.Item.TemplateID != TemplateIDs.DictionaryEntry))
            {
                Sitecore.Globalization.ItemEventHandler itemEventHandler = new Sitecore.Globalization.ItemEventHandler();
                string str = (string)itemEventHandler.GetType().InvokeMember("OnVersionRemovedRemote", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, (Binder)null, (object)itemEventHandler, new object[2]
                {
                        sender,
                        (object) args
                });

                var parameter = itemSavedRemoteEventArgs.Item;

                if (parameter.TemplateID == TemplateIDs.DictionaryEntry && parameter.Versions.Count == 0 && IsItemFallbackEnabled(parameter))
                {
                    DictionaryDomain dictionaryDomainForEntry = this.FindDictionaryDomainForEntry(parameter, parameter.Parent);
                    if (dictionaryDomainForEntry != null)
                    {
                        using (new LanguageFallbackItemSwitcher(new bool?(true)))
                        {
                            var fallbackItem = parameter.GetFallbackItem();
                            if (fallbackItem != null)
                                CachePhraseVersionRemoved(fallbackItem[FieldIDs.DictionaryKey], parameter, dictionaryDomainForEntry);

                        }
                    }
                }
            }         
        }

        private void OnVersionRemoved(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull((object)args, "args");
            Item parameter = Event.ExtractParameter(args, 0) as Item;
            if (parameter == null)
                return;

            Sitecore.Globalization.ItemEventHandler itemEventHandler = new Sitecore.Globalization.ItemEventHandler();
            string str = (string)itemEventHandler.GetType().InvokeMember("OnVersionRemoved", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, (Binder)null, (object)itemEventHandler, new object[2]
            {
                sender,
                (object) args
            });

            if (parameter.TemplateID == TemplateIDs.DictionaryEntry && parameter.Versions.Count == 0 && IsItemFallbackEnabled(parameter))
            {
                DictionaryDomain dictionaryDomainForEntry = this.FindDictionaryDomainForEntry(parameter, parameter.Parent);
                if (dictionaryDomainForEntry != null)
                {
                    using (new LanguageFallbackItemSwitcher(new bool?(true)))
                    {
                        CachePhraseVersionRemoved(parameter[FieldIDs.DictionaryKey], parameter, dictionaryDomainForEntry);
                    }
                }
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

        private void CachePhraseVersionRemoved(string dictionaryKey, Item item, DictionaryDomain domain)
        {
            Item obj = item.Database.GetItem(item.ID, item.Language, item.Version);
            if (obj != null)
            {
                var fallbackItem = obj.GetFallbackItem();
                if (fallbackItem != null)
                {
                    obj.Fields[FieldIDs.DictionaryPhrase].GetValue(true, true);
                    Translate.CachePhrase(dictionaryKey, fallbackItem[FieldIDs.DictionaryPhrase], obj.Language, domain);

                    Translate translate = new Translate();
                    string str = (string)translate.GetType().InvokeMember("Save", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, (Binder)null, (object)translate, new object[0]);
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

        private bool IsItemFallbackEnabled(Item item)
        {
            if (item != null)
            {
                if (!StandardValuesManager.IsStandardValuesHolder(item))
                {
                    using (new LanguageFallbackItemSwitcher(false))
                    {
                        return item.Fields[FieldIDs.EnableItemFallback].GetValue(true, true, false) == "1";
                    }
                }
                return item.Fields[FieldIDs.EnableItemFallback].GetValue(false) == "1";
            }
            return false;
        }
    }
}