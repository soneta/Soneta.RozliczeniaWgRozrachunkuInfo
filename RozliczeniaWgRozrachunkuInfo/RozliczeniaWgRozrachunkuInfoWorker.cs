using RozliczeniaWgRozrachunkuInfo;
using Soneta.Business;
using Soneta.Kasa;
using Soneta.Ksiega;
using Soneta.Types;
using System.Collections.Generic;

[assembly: Worker(typeof(RozliczeniaWgRozrachunkuInfoWorker), typeof(ElemOpisuAnalitycznego))]

namespace RozliczeniaWgRozrachunkuInfo
{
    public class RozliczeniaWgRozrachunkuInfoWorker
    {
        private ElemOpisuAnalitycznego _elemOpisuAnalitycznego;
        private OpisAnalityczny.Params _params;
        private Dictionary<ElemOpisuAnalitycznego, WartosciRozliczen> _elemOpisuAnalitycznegoDict;

        public RozliczeniaWgRozrachunkuInfoWorker()
        {
            _elemOpisuAnalitycznegoDict = new Dictionary<ElemOpisuAnalitycznego, WartosciRozliczen>();
        }

        [Context]
        public OpisAnalityczny.Params Params
        {
            get { return _params; }
            set { _params = value; }
        }

        [Context]
        public ElemOpisuAnalitycznego ElemOpisuAnalitycznego
        {
            get { return _elemOpisuAnalitycznego; }
            set
            {
                _elemOpisuAnalitycznego = value;
                if (_elemOpisuAnalitycznegoDict.ContainsKey(value))
                {
                    if (_elemOpisuAnalitycznegoDict[value].Data != Params.Data)
                        _elemOpisuAnalitycznegoDict[value].Data = Params.Data;
                    return;
                }

                _elemOpisuAnalitycznegoDict[value] = new WartosciRozliczen(value, Params.Data);
            }
        }

        public class WartosciRozliczen
        {
            private readonly ElemOpisuAnalitycznego _elemOpisuAnalitycznego;
            private Date _data;

            public WartosciRozliczen(ElemOpisuAnalitycznego elemOpisuAnalitycznego, Date data)
            {
                _elemOpisuAnalitycznego = elemOpisuAnalitycznego;
                _data = data;
                LiczRozliczenia();
            }

            public Date Data
            {
                get { return _data; }
                set 
                { 
                    _data = value;
                    LiczRozliczenia();
                }
            }

            internal Currency rozliczonoNaleznosc;
            internal Currency rozliczonoNaleznoscPLNHist;
            internal Currency rozliczonoZobowiazanie;
            internal Currency rozliczonoZobowiazaniePLNHist;
            internal Currency rozliczonoSaldo;
            internal Currency rozliczonoSaldoPLNHist;
            internal Currency doRozliczeniaNaleznosc;
            internal Currency doRozliczeniaNaleznoscPLNHist;
            internal Currency doRozliczeniaZobowiazanie;
            internal Currency doRozliczeniaZobowiazaniePLNHist;
            internal Currency doRozliczeniaSaldo;
            internal Currency doRozliczeniaSaldoPLNHist;
            internal IPodmiotKasowy podmiot;
            internal string numerDokumentu;
            internal Date termin;

            #region Licz

            private void LiczRozliczenia()
            {
                if (_elemOpisuAnalitycznego.Zrodlo != null)
                {
                    var platnosc = _elemOpisuAnalitycznego.Zrodlo as Soneta.Kasa.Platnosc;
                    var zaplata = _elemOpisuAnalitycznego.Zrodlo as Soneta.Kasa.Zaplata;

                    if (platnosc != null && platnosc.StanRozliczenia != StanRozliczenia.NiePodlega)
                    {
                        if (platnosc.Kwota.Symbol == _elemOpisuAnalitycznego.Kwota.Symbol)
                            LiczPlatnoscZgodnySymbol(platnosc);
                        else
                            LiczPlatnoscNiezgodnySymbol(platnosc);

                        podmiot = platnosc.Podmiot;
                        numerDokumentu = platnosc.NumerDokumentu;
                        termin = platnosc.Termin;
                    }

                    if (zaplata != null && zaplata.StanRozliczenia != StanRozliczenia.NiePodlega)
                    {
                        if (zaplata.Kwota.Symbol == _elemOpisuAnalitycznego.Kwota.Symbol)
                            LiczZaplateZgodnySymbol(zaplata);
                        else
                            LiczZaplateNiezgodnySymbol(zaplata);

                        podmiot = zaplata.Podmiot;
                        numerDokumentu = zaplata.NumerDokumentu;
                        termin = Date.Empty;
                    }

                    LiczSaldo();
                }
            }

            private void LiczPlatnoscZgodnySymbol(Platnosc platnosc)
            {
                Currency iloscRozliczonego = Currency.Zero;
                foreach (RozliczenieSP rozliczenia in platnosc.Rozliczenia)
                {
                    if (rozliczenia.Data <= _data)
                        iloscRozliczonego += rozliczenia.KwotaDokumentu;
                }

                if (platnosc.Typ == TypRozrachunku.Zobowiązanie)
                {
                    rozliczonoZobowiazanie = (iloscRozliczonego / platnosc.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaZobowiazanie = _elemOpisuAnalitycznego.Kwota - rozliczonoZobowiazanie;
                }
                else if (platnosc.Typ == TypRozrachunku.Należność)
                {
                    rozliczonoNaleznosc = (iloscRozliczonego / platnosc.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaNaleznosc = _elemOpisuAnalitycznego.Kwota - rozliczonoNaleznosc;
                }
            }

            private void LiczPlatnoscNiezgodnySymbol(Platnosc platnosc)
            {
                Currency iloscRozliczonego = Currency.Zero;
                foreach (RozliczenieSP rozliczenia in platnosc.Rozliczenia)
                {
                    if (rozliczenia.Data <= _data)
                        iloscRozliczonego += new Currency(rozliczenia.KwotaDokumentu.Value * (decimal)platnosc.Kurs, _elemOpisuAnalitycznego.Kwota.Symbol);
                }

                Currency platnoscKwota = new Currency(platnosc.Kwota.Value * (decimal)platnosc.Kurs, _elemOpisuAnalitycznego.Kwota.Symbol);

                if (platnosc.Typ == TypRozrachunku.Zobowiązanie)
                {
                    rozliczonoZobowiazaniePLNHist = (iloscRozliczonego / platnoscKwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaZobowiazaniePLNHist = _elemOpisuAnalitycznego.Kwota - rozliczonoZobowiazaniePLNHist;
                }
                else if (platnosc.Typ == TypRozrachunku.Należność)
                {
                    rozliczonoNaleznoscPLNHist = (iloscRozliczonego / platnoscKwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaNaleznoscPLNHist = _elemOpisuAnalitycznego.Kwota - rozliczonoNaleznoscPLNHist;
                }
            }

            private void LiczZaplateZgodnySymbol(Zaplata zaplata)
            {
                Currency iloscRozliczonego = Currency.Zero;
                foreach (RozliczenieSP rozliczenia in zaplata.Rozliczenia)
                {
                    if (rozliczenia.Data <= _data)
                        iloscRozliczonego += rozliczenia.KwotaDokumentu;
                }

                if (zaplata.Typ == TypRozrachunku.Wpłata)
                {
                    rozliczonoZobowiazanie = (iloscRozliczonego / zaplata.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaZobowiazanie = _elemOpisuAnalitycznego.Kwota - rozliczonoZobowiazanie;
                }
                else if (zaplata.Typ == TypRozrachunku.Wypłata)
                {
                    rozliczonoNaleznosc = (iloscRozliczonego / zaplata.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaNaleznosc = _elemOpisuAnalitycznego.Kwota - rozliczonoNaleznosc;
                }
            }

            private void LiczZaplateNiezgodnySymbol(Zaplata zaplata)
            {
                Currency iloscRozliczonego = Currency.Zero;
                foreach (RozliczenieSP rozliczenia in zaplata.Rozliczenia)
                {
                    if (rozliczenia.Data <= _data)
                        iloscRozliczonego += new Currency(rozliczenia.KwotaDokumentu.Value * (decimal)zaplata.Kurs, _elemOpisuAnalitycznego.Kwota.Symbol);
                }

                Currency zaplataKwota = new Currency(zaplata.Kwota.Value * (decimal)zaplata.Kurs, _elemOpisuAnalitycznego.Kwota.Symbol);

                if (zaplata.Typ == TypRozrachunku.Wpłata)
                {
                    rozliczonoZobowiazaniePLNHist = (iloscRozliczonego / zaplataKwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaZobowiazaniePLNHist = _elemOpisuAnalitycznego.Kwota - rozliczonoZobowiazaniePLNHist;
                }
                else if (zaplata.Typ == TypRozrachunku.Wypłata)
                {
                    rozliczonoNaleznoscPLNHist = (iloscRozliczonego / zaplataKwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaNaleznoscPLNHist = _elemOpisuAnalitycznego.Kwota - rozliczonoNaleznoscPLNHist;
                }
            }

            private void LiczSaldo()
            {
                rozliczonoSaldo = rozliczonoNaleznosc - rozliczonoZobowiazanie >= 0
                    ? rozliczonoNaleznosc - rozliczonoZobowiazanie
                    : -(rozliczonoNaleznosc - rozliczonoZobowiazanie);

                doRozliczeniaSaldo = doRozliczeniaNaleznosc - doRozliczeniaZobowiazanie >= 0
                    ? doRozliczeniaNaleznosc - doRozliczeniaZobowiazanie
                    : -(doRozliczeniaNaleznosc - doRozliczeniaZobowiazanie);

                rozliczonoSaldoPLNHist = rozliczonoNaleznoscPLNHist - rozliczonoZobowiazaniePLNHist >= 0
                    ? rozliczonoNaleznoscPLNHist - rozliczonoZobowiazaniePLNHist
                    : -(rozliczonoNaleznoscPLNHist - rozliczonoZobowiazaniePLNHist);

                doRozliczeniaSaldoPLNHist = doRozliczeniaNaleznoscPLNHist - doRozliczeniaZobowiazaniePLNHist >= 0
                    ? doRozliczeniaNaleznoscPLNHist - doRozliczeniaZobowiazaniePLNHist
                    : -(doRozliczeniaNaleznoscPLNHist - doRozliczeniaZobowiazaniePLNHist);
            }

            #endregion
        }

        [Caption("Rozliczono/Należność")]
        public Currency RozliczonoNaleznosc => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].rozliczonoNaleznosc;

        [Caption("Rozliczono/NależnośćPLNHist")]
        public Currency RozliczonoNależnośćPLNHist => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].rozliczonoNaleznoscPLNHist;

        [Caption("Rozliczono/Zobowiązanie")]
        public Currency RozliczonoZobowiazanie => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].rozliczonoZobowiazanie;

        [Caption("Rozliczono/ZobowiązaniePLNHis")]
        public Currency RozliczonoZobowiazaniePLNHis => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].rozliczonoZobowiazaniePLNHist;

        [Caption("Rozliczono/Saldo")]
        public Currency RozliczonoSaldo => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].rozliczonoSaldo;

        [Caption("Rozliczono/SaldoPLNHist")]
        public Currency RozliczonoSaldoPLNHist => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].rozliczonoSaldoPLNHist;

        [Caption("Do rozliczenia/Należność")]
        public Currency DoRozliczeniaNaleznosc => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].doRozliczeniaNaleznosc;

        [Caption("Do rozliczenia/NależnośćPLNHist")]
        public Currency DoRozliczeniaNaleznoscPLNHist => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].doRozliczeniaNaleznoscPLNHist;

        [Caption("Do rozliczenia/Zobowiązanie")]
        public Currency DoRozliczeniaZobowiazanie => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].doRozliczeniaZobowiazanie;

        [Caption("Do rozliczenia/ZobowiązaniePLNHist")]
        public Currency DoRozliczeniaZobowiazaniePLNHist => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].doRozliczeniaZobowiazaniePLNHist;

        [Caption("Do rozliczenia/Saldo")]
        public Currency DoRozliczeniaSaldo => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].doRozliczeniaSaldo;

        [Caption("Do rozliczenia/SaldoPLNHist")]
        public Currency DoRozliczeniaSaldoPLNHist => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].doRozliczeniaSaldoPLNHist;
        
        [Caption("Podmiot")]
        public IPodmiotKasowy Podmiot => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].podmiot;
        
        [Caption("Numer dokumentu")]
        public string NumerDokumentu => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].numerDokumentu;
        
        [Caption("Termin")]
        public Date Termin => _elemOpisuAnalitycznegoDict[ElemOpisuAnalitycznego].termin;
    }
}
