using Mono.CSharp;
using Soneta.Business;
using Soneta.Core;
using Soneta.Kasa;
using Soneta.Ksiega;
using Soneta.PulpitHandlowca;
using Soneta.Tools;
using Soneta.Types;
using System;
using static Soneta.Place.SumyElementow;
using static Soneta.Place.WypElementPożyczka;

namespace Soneta.RozliczeniaWgRozrachunkuInfo.Extender
{
    public class OpisAnalityczny2
    {
        public class Params : OpisAnalityczny.Params
        {
            private readonly string _key = "Opis analityczny 2".TranslateIgnore();

            private TypRozIndeksu indeks;
            private RodzajDokumentów rodzaj;
            private TypRozDokumentów dokumenty;

            public Params(Context context) : base(context)
            {
            }

            [Caption("Indeks")]
            public TypRozIndeksu Indeks
            {
                get { return indeks; }
                set
                {
                    if (indeks != value)
                    {
                        indeks = value;
                        MarkChanged();
                    }
                }
            }

            [Caption("Typ")]
            public TypRozDokumentów Dokumenty
            {
                get { return dokumenty; }
                set
                {
                    if(dokumenty != value)
                    {
                        dokumenty = value;
                        MarkChanged();
                    }
                }
            }

            [Caption("Rodzaj")]
            public RodzajDokumentów Rodzaj
            {
                get { return KontrahentPulpituHandlowcaManager.JestLicencja(Session) ? RodzajDokumentów.Płatności : rodzaj; }
                set
                {
                    if(rodzaj != value)
                    {
                        rodzaj = value;
                        MarkChanged();
                    }
                }
            }

            private void MarkChanged()
            {
                Save();
                OnChanged(EventArgs.Empty);
            }

            private void Save()
            {
                SaveProperty("Indeks", _key);
            }

            public RowCondition CreateNewCondition()
            {
                RowCondition empty = RowCondition.Empty;
                decimal zero = 0m;

                switch (Indeks)
                {
                    case TypRozIndeksu.Rozliczone:
                        empty &= (RowCondition)new FieldCondition.Equal("Workers.RozliczeniaWgRozrachunkuInfo.DoRozliczeniaSaldo.Value", zero);
                        break;
                    case TypRozIndeksu.Nierozliczone:
                        empty &= (RowCondition)new FieldCondition.NotEqual("Workers.RozliczeniaWgRozrachunkuInfo.DoRozliczeniaSaldo.Value", zero);
                        break;
                }

                //switch (Dokumenty)
                //{
                //    case TypRozDokumentów.Należności:
                //        empty &= (RowCondition)new FieldCondition.Equal("Workers.RozliczeniaWgRozrachunkuInfo.DokumentTyp", TypRozrachunku.Należność);
                //        break;
                //    case TypRozDokumentów.Zobowiązania:
                //        empty &= (RowCondition)new FieldCondition.Equal("Workers.RozliczeniaWgRozrachunkuInfo.DokumentTyp", TypRozrachunku.Zobowiązanie);
                //        break;
                //}
                empty &= ZakresEx(
                    Dokumenty != TypRozDokumentów.Zobowiązania && Rodzaj != RodzajDokumentów.Zapłaty,
                    Dokumenty != TypRozDokumentów.Należności && Rodzaj != RodzajDokumentów.Zapłaty,
                    Dokumenty != TypRozDokumentów.Należności && Rodzaj != RodzajDokumentów.Płatności,
                    Dokumenty != TypRozDokumentów.Zobowiązania && Rodzaj != RodzajDokumentów.Płatności);

                switch (Rodzaj)
                {
                    case RodzajDokumentów.Płatności:
                        empty &= (RowCondition)new FieldCondition.TypeOf("Zrodlo", "Platnosci");
                        break;
                    case RodzajDokumentów.Zapłaty:
                        empty &= (RowCondition)new FieldCondition.TypeOf("Zrodlo", "Zaplaty");
                        break;
                }

                return empty;
            }

            private RowCondition ZakresEx(bool naleznosci, bool zobowiazania, bool wplaty, bool wyplaty)
            {
                RowCondition condition = RowCondition.Empty;
                if (naleznosci && zobowiazania && wplaty && wyplaty)
                    return condition;

                if (zobowiazania)
                    condition |= new FieldCondition.Equal("Workers.RozliczeniaWgRozrachunkuInfo.DokumentTyp", TypRozrachunku.Zobowiązanie);
                if (naleznosci)
                    condition |= new FieldCondition.Equal("Workers.RozliczeniaWgRozrachunkuInfo.DokumentTyp", TypRozrachunku.Należność);
                if (wplaty)
                    condition |= new FieldCondition.Equal("Workers.RozliczeniaWgRozrachunkuInfo.DokumentTyp", TypRozrachunku.Wpłata);
                if (wyplaty)
                    condition |= new FieldCondition.Equal("Workers.RozliczeniaWgRozrachunkuInfo.DokumentTyp", TypRozrachunku.Wypłata);

                return condition;
            }
        }
    }
}
