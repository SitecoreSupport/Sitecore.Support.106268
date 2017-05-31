using System;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Support.Globalization;

namespace Sitecore.Support.Publishing
{
    public class DictionaryCacheUpdater : ItemEventHandler
    {
        public void UpdateDicCache(object sender, EventArgs args)
        {
            foreach (string publishingTarget in (args as PublishEndRemoteEventArgs).PublishingTargets)
            {
                Item obj = Context.Database.GetItem(ID.Parse(publishingTarget));
                if (!(obj.TemplateID != TemplateIDs.DictionaryEntry))
                    this.UpdateCacheEntries(obj[FieldIDs.DictionaryKey], obj[FieldIDs.DictionaryKey], obj);
            }
            Log.Info("UpdateDicCache done.", (object)this);
        }
    }
}