using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json;

namespace WebhostUniversalWindows.Comments
{
    public static class CommentSettings
    {
        private static ApplicationDataContainer LocalSettings
        {
            get
            {
                return ApplicationData.Current.LocalSettings;
            }
        }

        /// <summary>
        /// Save a json serializable object to a temporary file.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="filename"></param>
        /// <param name="objectType"></param>
        public static async void SaveTemporaryFileAsync(object obj, String filename, Type objectType)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(obj, objectType, new JsonSerializerSettings() { DateFormatHandling = DateFormatHandling.IsoDateFormat }));
        }

        public static async Task<object> LoadFromTemporaryFileAsync(String fileName, Type objectType)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
            String data = await FileIO.ReadTextAsync(file);

            return JsonConvert.DeserializeObject(data, objectType, new JsonSerializerSettings() { DateFormatHandling = DateFormatHandling.IsoDateFormat });
        }

        /// <summary>
        /// Local application cache for the current student comment being edited.
        /// </summary>
        public static CommentInfo CurrentStudentComment
        {
            get
            {
                if (LocalSettings.Values.ContainsKey("CurrentStudentComment"))
                    return CommentInfo.FromJson((String)LocalSettings.Values["CurrentStudentComment"]);

                return null;
            }
            set
            {
                if (value == null)
                    LocalSettings.Values.Remove("CurrentStudentComment");
                else if (LocalSettings.Values.ContainsKey("CurrentStudentComment"))
                    LocalSettings.Values["CurrentStudentComment"] = value.ToString();
                else
                    LocalSettings.Values.Add("CurrentStudentComment", value.ToString());
            }
        }
        
        /// <summary>
        /// get the currently cached comment header paragraph.
        /// </summary>
        public static CommentHeaderInfo CurrentHeaderParagraph
        {
            get
            {
                if (LocalSettings.Values.ContainsKey("CurrentHeaderParagraph"))
                    return CommentHeaderInfo.FromJson((String)LocalSettings.Values["CurrentHeaderParagraph"]);

                return null;
            }
            set
            {
                if (value == null)
                    LocalSettings.Values.Remove("CurrentHeaderParagraph");
                else if (LocalSettings.Values.ContainsKey("CurrentHeaderParagraph"))
                    LocalSettings.Values["CurrentHeaderParagraph"] = value.ToString();
                else
                {
                    LocalSettings.Values.Add("CurrentHeaderParagraph", value.ToString());
                }
            }
        }
    }
}
