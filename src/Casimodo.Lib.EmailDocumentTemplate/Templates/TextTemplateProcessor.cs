using System;
using System.Text;
#nullable enable

namespace Casimodo.Lib.Templates
{
    public class TextTemplateProcessor : TemplateProcessor
    {
        public TextTemplateProcessor(TemplateContext context)
            : base(context)
        { }

        public StringBuilder TextBuilder { get; } = new();

        public void Clear()
        {
            TextBuilder.Length = 0;
        }

        public override void RemoveValue()
        {
            // NOP
        }

        public override void SetText(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                TextBuilder.Append(value);
            }
        }

        public override void SetImage(Guid? imageFileId, bool removeIfEmpty = false)
        {
            throw new NotSupportedException();
        }
    }
}
