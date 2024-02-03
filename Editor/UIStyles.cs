using UnityEditor;
using UnityEngine;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    static class  UIStyles
    {
        public static GUIStyle IntTextStyle
        {
            get
            {
                if (IntTextStyleCache == null)
                {
                    IntTextStyleCache = new GUIStyle(EditorStyles.label);
                    IntTextStyleCache.normal.textColor = Color.red;
                }
                return IntTextStyleCache;
            }
        }
        static GUIStyle IntTextStyleCache;


        public static GUIStyle FloatTextStyle
        {
            get
            {
                if (FloatTextStyleCache == null)
                {
                    FloatTextStyleCache = new GUIStyle(EditorStyles.label);
                    FloatTextStyleCache.normal.textColor = Color.green;
                }
                return FloatTextStyleCache;
            }
        }
        static GUIStyle FloatTextStyleCache;

        public static GUIStyle IntFieldStyle
        {
            get
            {
                if (IntFieldStyleCache == null)
                {
                    IntFieldStyleCache = new GUIStyle(EditorStyles.textField);
                    IntFieldStyleCache.normal.textColor = Color.red;
                }
                return IntFieldStyleCache;
            }
        }
        static GUIStyle IntFieldStyleCache;


        public static GUIStyle FloatFieldStyle
        {
            get
            {
                if (FloatFieldStyleCache == null)
                {
                    FloatFieldStyleCache = new GUIStyle(EditorStyles.textField);
                    FloatFieldStyleCache.normal.textColor = Color.green;
                }
                return FloatFieldStyleCache;
            }
        }
        static GUIStyle FloatFieldStyleCache;
    }
}
