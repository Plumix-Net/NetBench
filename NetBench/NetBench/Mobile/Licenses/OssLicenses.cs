using Plumix.Foundation;

namespace NetBench.Mobile.Licenses;

/// <summary>
/// Лицензии того, что входит в мобильную сборку, — для LicensePage (Plumix.Material).
/// Тексты — встроенные ресурсы Mobile/Licenses/*.txt; держатели прав — из LICENSE/nuspec пакетов.
/// При добавлении пакета в мобильную сборку — добавить его сюда.
/// </summary>
public static class OssLicenses
{
    private static int _registered;

    private static readonly (string[] Packages, string Copyright, string Text)[] _entries =
    [
        (["Plumix", "Plumix.Material", "Plumix.Cupertino", "Plumix.Android"],
            "Copyright (c) 2024–2026 Egor Zheludkov", "MIT"),
        (["Plumix.Bloc", "Plumix.Slang"], "Copyright (c) 2024–2026 Plumix Contributors", "MIT"),
        (["Avalonia"], "Copyright (c) AvaloniaUI OÜ", "MIT"),
        (["SkiaSharp", "HarfBuzzSharp"],
            "Copyright (c) 2015-2016 Xamarin, Inc.\nCopyright (c) 2017-2018 Microsoft Corporation", "MIT"),
        (["Skia"], "Copyright (c) 2011 Google Inc. All rights reserved.", "BSD-3-Clause"),
        (["HarfBuzz"], "Copyright © 2010-2022 Google, Inc. and the HarfBuzz contributors", "HarfBuzz"),
        (["Inter"], "Copyright (c) 2020 The Inter Project Authors (https://github.com/rsms/inter)", "OFL-1.1"),
        (["HdrHistogram"],
            "Written by Gil Tene, Michael Barker, and Matt Warren, and released to the public domain,\n"
            + "as explained at http://creativecommons.org/publicdomain/zero/1.0/. Also provided under\n"
            + "the BSD 2-Clause License:\n\nCopyright (c) 2012, 2013, 2014 Gil Tene\n"
            + "Copyright (c) 2014 Michael Barker\nCopyright (c) 2014 Matt Warren\nAll rights reserved.",
            "BSD-2-Clause"),
        (["Slang.Net"], "Copyright (c) 2020-2024 Tien Do Nam\nCopyright (c) 2024 Egor Zheludkov", "MIT"),
        (["MaterialTheming"], "Copyright (c) Muckenbatscher", "MIT"),
        ([".NET"], "Copyright (c) .NET Foundation and Contributors", "MIT"),
    ];

    /// <summary>Регистрирует записи в LicenseRegistry; повторный вызов ничего не делает.</summary>
    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
            return;

        LicenseRegistry.AddLicense(static () => _entries.Select(LicenseEntry (entry) =>
            new LicenseEntryWithLineBreaks(entry.Packages, $"{entry.Copyright}\n\n{ReadText(entry.Text)}")));
    }

    private static string ReadText(string name)
    {
        using var stream = typeof(OssLicenses).Assembly.GetManifestResourceStream($"NetBench.Licenses.{name}.txt")
            ?? throw new InvalidOperationException($"License text {name} is not embedded");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
