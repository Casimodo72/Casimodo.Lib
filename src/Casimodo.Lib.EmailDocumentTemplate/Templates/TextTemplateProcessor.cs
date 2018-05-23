using System;
using System.Text;

namespace Casimodo.Lib.Templates
{
    public class TextTemplateProcessor : TemplateProcessor
    {
        public TextTemplateProcessor()
        { }

        public StringBuilder TextBuilder { get; private set; } = new StringBuilder();

        public void Clear()
        {
            TextBuilder.Length = 0;
        }

        public override void RemoveValue()
        {
            // NOP
        }

        public override void SetText(string value)
        {
            TextBuilder.Append(value);
        }

        public override void SetImage(Guid? imageFileId, bool removeIfEmpty = false)
        {
            throw new NotSupportedException();
        }
    }
}
