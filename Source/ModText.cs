using System;
using System.Globalization;

namespace RimDroid.Readability
{
    // Original, game-independent formatting. The mod binds Lookup to Verse's Keyed translator.
    internal static class ModText
    {
        internal static Func<string, string> Lookup;
        internal static Func<object> Language;
        internal static object CurrentLanguage { get { return Language == null ? null : Language(); } }
        internal static string Get(string key, params object[] arguments)
        {
            string fullKey = "RDR_" + key;
            string template = Lookup == null ? fullKey : Lookup(fullKey);
            return arguments.Length == 0 ? template : string.Format(CultureInfo.CurrentCulture, template, arguments);
        }
        internal static LocalizedText Message(string key, params object[] arguments)
        { return new LocalizedText(key, arguments); }
        internal static LocalizedText Error(string context, Exception error)
        {
            LocalizedException localized = error as LocalizedException;
            object detail = localized == null ? (object)(error.GetType().Name + ": " + error.Message)
                : Message("ErrorFormat", nameof(InvalidOperationException), localized.Text);
            return Message("ErrorFormat", Message(context), detail);
        }
    }

    internal sealed class LocalizedText
    {
        private readonly string key;
        private readonly object[] arguments;
        internal LocalizedText(string key, params object[] arguments) { this.key = key; this.arguments = arguments; }
        public override string ToString() { return ModText.Get(key, arguments); }
    }

    internal sealed class LocalizedException : InvalidOperationException
    {
        internal readonly LocalizedText Text;
        internal LocalizedException(LocalizedText text) { Text = text; }
        public override string Message { get { return Text.ToString(); } }
    }
}
