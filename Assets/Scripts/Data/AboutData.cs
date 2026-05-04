using System;
using System.Collections.Generic;
using UnityEngine;

namespace UstAldanQuiz.Data
{
    [Serializable]
    public class AboutData
    {
        public string       title;
        public string       description;
        public string       developer;
        public string       version;
        public string       year;
        public List<Contact> contacts = new List<Contact>();
        public List<string>  partners = new List<string>();

        [Serializable]
        public class Contact
        {
            public string label; // locale key
            public string value; // literal (url, email)
        }

        public static AboutData Load()
        {
            var asset = Resources.Load<TextAsset>("about");
            if (asset == null)
            {
                Debug.LogWarning("[AboutData] about.json не найден в Resources.");
                return new AboutData();
            }
            return JsonUtility.FromJson<AboutData>(asset.text);
        }
    }
}
