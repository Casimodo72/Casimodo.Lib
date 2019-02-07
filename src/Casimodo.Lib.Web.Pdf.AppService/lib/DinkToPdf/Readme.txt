DinkToPdf:

Custom build from sources on 2019-02-06 (release configuration) (v1.0.8 on NuGet)

With following changes in DinkToPdf.ObjectSettings:
Added and using RawContent in order to feed the converter also with a byte array instead of a string.
In my scenario the input is already a byte array.

public byte[] RawContent { get; set; }

public byte[] GetContent()
{
    if (RawContent != null)
        return RawContent;

    if (HtmlContent == null)
    {
        return new byte[0];
    }

    return Encoding.UTF8.GetBytes(HtmlContent);
}