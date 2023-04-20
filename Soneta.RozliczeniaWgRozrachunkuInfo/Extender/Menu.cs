using Soneta.Business.Licence;
using Soneta.Business.UI;
using Soneta.RozliczeniaWgRozrachunkuInfo.Extender;

[assembly: FolderView("Ewidencja dokumentów/Zestawienie dokumentów info",
    Priority = 115, GroupIndex = 1,
    TableName = "ElemOpisuAnalitycznego",
    Description = "Zestawienie dokumentów wg opisu analitycznego info",
    IconName = "stos",
    ViewType = typeof(OpisAnalityczny2ViewInfo),
    Contexts = new object[] { LicencjeModułu.KS | LicencjeModułu.KPiR | LicencjeModułu.PRJ })]