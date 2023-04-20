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
                        if (platnosc.Kwota.Symbol == _elemOpisuAnalitycznego.Kwota.Symbol && platnosc.Kwota.Symbol == _elemOpisuAnalitycznego.KwotaDodatkowa.Symbol)
                            LiczPlatnoscZgodnySymbol(platnosc);
                        else if (platnosc.Kwota.Symbol == _elemOpisuAnalitycznego.Kwota.Symbol && platnosc.Kwota.Symbol != _elemOpisuAnalitycznego.KwotaDodatkowa.Symbol)
                            LiczPlatnoscNiezgodnySymbolKwotaDodatkowa(platnosc);
                        else
                            LiczPlatnoscNiezgodnySymbol(platnosc);

                        podmiot = platnosc.Podmiot;
                        numerDokumentu = platnosc.NumerDokumentu;
                        termin = platnosc.Termin;
                    }

                    if (zaplata != null && zaplata.StanRozliczenia != StanRozliczenia.NiePodlega)
                    {
                        if(zaplata.Kwota.Symbol == _elemOpisuAnalitycznego.Kwota.Symbol && zaplata.Kwota.Symbol == _elemOpisuAnalitycznego.KwotaDodatkowa.Symbol)
                            LiczZaplateZgodnySymbol(zaplata);
                        else if (zaplata.Kwota.Symbol == _elemOpisuAnalitycznego.Kwota.Symbol && zaplata.Kwota.Symbol != _elemOpisuAnalitycznego.KwotaDodatkowa.Symbol)
                            LiczZaplateNiezgodnySymbolKwotaDodatkowa(zaplata);
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
                Currency iloscRozliczonego = new Currency(Currency.Zero.Value, _elemOpisuAnalitycznego.Kwota.Symbol);
                foreach (RozliczenieSP rozliczenia in platnosc.Rozliczenia)
                {
                    if (rozliczenia.Data <= _data)
                        iloscRozliczonego += rozliczenia.KwotaDokumentu;
                }

                if (platnosc.Typ == TypRozrachunku.Zobowiązanie)
                {
                    rozliczonoZobowiazanie = (iloscRozliczonego / platnosc.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaZobowiazanie = _elemOpisuAnalitycznego.Kwota - rozliczonoZobowiazanie;
                    if (rozliczonoZobowiazanie.Symbol == Currency.SystemSymbol && doRozliczeniaZobowiazanie.Symbol == Currency.SystemSymbol)
                    {
                        rozliczonoZobowiazaniePLNHist = rozliczonoZobowiazanie;
                        doRozliczeniaZobowiazaniePLNHist = doRozliczeniaZobowiazanie;
                    }
                }
                else if (platnosc.Typ == TypRozrachunku.Należność)
                {
                    rozliczonoNaleznosc = (iloscRozliczonego / platnosc.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaNaleznosc = _elemOpisuAnalitycznego.Kwota - rozliczonoNaleznosc;
                    if (rozliczonoNaleznosc.Symbol == Currency.SystemSymbol && doRozliczeniaNaleznosc.Symbol == Currency.SystemSymbol)
                    {
                        rozliczonoNaleznoscPLNHist = rozliczonoNaleznosc;
                        doRozliczeniaNaleznoscPLNHist = doRozliczeniaNaleznosc;
                    }
                }
            }

            private void LiczPlatnoscNiezgodnySymbolKwotaDodatkowa(Platnosc platnosc)
            {
                Currency iloscRozliczonegoPLN = Currency.Zero;
                Currency iloscRozliczonego = new Currency(Currency.Zero.Value, _elemOpisuAnalitycznego.Kwota.Symbol);
                foreach (RozliczenieSP rozliczenia in platnosc.Rozliczenia)
                {
                    if (rozliczenia.Data <= _data)
                    {
                        iloscRozliczonegoPLN += new Currency(rozliczenia.KwotaDokumentu.Value * (decimal)platnosc.Kurs, Currency.SystemSymbol);
                        iloscRozliczonego += rozliczenia.KwotaDokumentu;
                    }
                }

                Currency platnoscKwota = new Currency(platnosc.Kwota.Value * (decimal)platnosc.Kurs, Currency.SystemSymbol);
                Currency elementOAKwota = new Currency((double)_elemOpisuAnalitycznego.Kwota.Value * platnosc.Kurs, Currency.SystemSymbol);

                if (platnosc.Typ == TypRozrachunku.Zobowiązanie)
                {
                    rozliczonoZobowiazaniePLNHist = (iloscRozliczonegoPLN / platnoscKwota) * elementOAKwota;
                    doRozliczeniaZobowiazaniePLNHist = elementOAKwota - rozliczonoZobowiazaniePLNHist;
                    rozliczonoZobowiazanie = (iloscRozliczonego / platnosc.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaZobowiazanie = _elemOpisuAnalitycznego.Kwota - rozliczonoZobowiazanie;
                }
                else if (platnosc.Typ == TypRozrachunku.Należność)
                {
                    rozliczonoNaleznoscPLNHist = (iloscRozliczonegoPLN / platnoscKwota) * elementOAKwota;
                    doRozliczeniaNaleznoscPLNHist = elementOAKwota - rozliczonoNaleznoscPLNHist;
                    rozliczonoNaleznosc = (iloscRozliczonego / platnosc.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaNaleznosc = _elemOpisuAnalitycznego.Kwota - rozliczonoNaleznosc;
                }
            }

            private void LiczPlatnoscNiezgodnySymbol(Platnosc platnosc)
            {
                Currency iloscRozliczonegoPLN = Currency.Zero;
                foreach (RozliczenieSP rozliczenia in platnosc.Rozliczenia)
                {
                    if (rozliczenia.Data <= _data)
                        iloscRozliczonegoPLN += new Currency(rozliczenia.KwotaDokumentu.Value * (decimal)platnosc.Kurs, Currency.SystemSymbol);
                }

                Currency platnoscKwota = new Currency(platnosc.Kwota.Value * (decimal)platnosc.Kurs, Currency.SystemSymbol);
                Currency elementOAKwota = new Currency((double)_elemOpisuAnalitycznego.Kwota.Value * platnosc.Kurs, Currency.SystemSymbol);

                if (platnosc.Typ == TypRozrachunku.Zobowiązanie)
                {
                    rozliczonoZobowiazaniePLNHist = (iloscRozliczonegoPLN / platnoscKwota) * elementOAKwota;
                    doRozliczeniaZobowiazaniePLNHist = elementOAKwota - rozliczonoZobowiazaniePLNHist;
                }
                else if (platnosc.Typ == TypRozrachunku.Należność)
                {
                    rozliczonoNaleznoscPLNHist = (iloscRozliczonegoPLN / platnoscKwota) * elementOAKwota;
                    doRozliczeniaNaleznoscPLNHist = elementOAKwota - rozliczonoNaleznoscPLNHist;
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
                    if (rozliczonoZobowiazanie.Symbol == Currency.SystemSymbol && doRozliczeniaZobowiazanie.Symbol == Currency.SystemSymbol)
                    {
                        rozliczonoZobowiazaniePLNHist = rozliczonoZobowiazanie;
                        doRozliczeniaZobowiazaniePLNHist = doRozliczeniaZobowiazanie;
                    }
                }
                else if (zaplata.Typ == TypRozrachunku.Wypłata)
                {
                    rozliczonoNaleznosc = (iloscRozliczonego / zaplata.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaNaleznosc = _elemOpisuAnalitycznego.Kwota - rozliczonoNaleznosc;
                    if (rozliczonoNaleznosc.Symbol == Currency.SystemSymbol && doRozliczeniaNaleznosc.Symbol == Currency.SystemSymbol)
                    {
                        rozliczonoNaleznoscPLNHist = rozliczonoNaleznosc;
                        doRozliczeniaNaleznoscPLNHist = doRozliczeniaNaleznosc;
                    }
                }
            }

            private void LiczZaplateNiezgodnySymbolKwotaDodatkowa(Zaplata zaplata)
            {
                Currency iloscRozliczonegoPLN = Currency.Zero;
                Currency iloscRozliczonego = new Currency(Currency.Zero.Value, _elemOpisuAnalitycznego.Kwota.Symbol);

                foreach (RozliczenieSP rozliczenia in zaplata.Rozliczenia)
                {
                    if (rozliczenia.Data <= _data)
                    {
                        iloscRozliczonegoPLN += new Currency(rozliczenia.KwotaDokumentu.Value * (decimal)zaplata.Kurs, Currency.SystemSymbol);
                        iloscRozliczonego += rozliczenia.KwotaDokumentu;
                    }
                }

                Currency zaplataKwota = new Currency(zaplata.Kwota.Value * (decimal)zaplata.Kurs, Currency.SystemSymbol);
                Currency elementOAKwota = new Currency((double)_elemOpisuAnalitycznego.Kwota.Value * zaplata.Kurs, Currency.SystemSymbol);

                if (zaplata.Typ == TypRozrachunku.Wpłata)
                {
                    rozliczonoZobowiazaniePLNHist = (iloscRozliczonegoPLN / zaplataKwota) * elementOAKwota;
                    doRozliczeniaZobowiazaniePLNHist = elementOAKwota - rozliczonoZobowiazaniePLNHist;
                    rozliczonoZobowiazanie = (iloscRozliczonego / zaplata.Kwota) * _elemOpisuAnalitycznego.Kwota;
                    doRozliczeniaZobowiazanie = _elemOpisuAnalitycznego.Kwota - rozliczonoZobowiazanie;
                }
                else if (zaplata.Typ == TypRozrachunku.Wypłata)
                {
                    rozliczonoNaleznoscPLNHist = (iloscRozliczonegoPLN / zaplataKwota) * elementOAKwota;
                    doRozliczeniaNaleznoscPLNHist = elementOAKwota - rozliczonoNaleznoscPLNHist;
                    rozliczonoNaleznosc = (iloscRozliczonego / zaplataKwota) * elementOAKwota;
                    doRozliczeniaNaleznosc = elementOAKwota - rozliczonoNaleznosc;
                }
            }

            private void LiczZaplateNiezgodnySymbol(Zaplata zaplata)
            {
                Currency iloscRozliczonegoPLN = Currency.Zero;

                foreach (RozliczenieSP rozliczenia in zaplata.Rozliczenia)
                {
                    if (rozliczenia.Data <= _data)
                        iloscRozliczonegoPLN += new Currency(rozliczenia.KwotaDokumentu.Value * (decimal)zaplata.Kurs, Currency.SystemSymbol);
                }

                Currency zaplataKwota = new Currency(zaplata.Kwota.Value * (decimal)zaplata.Kurs, Currency.SystemSymbol);
                Currency elementOAKwota = new Currency((double)_elemOpisuAnalitycznego.Kwota.Value * zaplata.Kurs, Currency.SystemSymbol);

                if (zaplata.Typ == TypRozrachunku.Wpłata)
                {
                    rozliczonoZobowiazaniePLNHist = (iloscRozliczonegoPLN / zaplataKwota) * elementOAKwota;
                    doRozliczeniaZobowiazaniePLNHist = elementOAKwota - rozliczonoZobowiazaniePLNHist;
                }
                else if (zaplata.Typ == TypRozrachunku.Wypłata)
                {
                    rozliczonoNaleznoscPLNHist = (iloscRozliczonegoPLN / zaplataKwota) * elementOAKwota;
                    doRozliczeniaNaleznoscPLNHist = elementOAKwota - rozliczonoNaleznoscPLNHist;
                }
            }

            private void LiczSaldo()
            {
                rozliczonoSaldo = new Currency(rozliczonoNaleznosc.Value - rozliczonoZobowiazanie.Value >= 0
                    ? rozliczonoNaleznosc.Value - rozliczonoZobowiazanie.Value
                    : -(rozliczonoNaleznosc.Value - rozliczonoZobowiazanie.Value), _elemOpisuAnalitycznego.Kwota.Symbol);

                doRozliczeniaSaldo = new Currency(doRozliczeniaNaleznosc.Value - doRozliczeniaZobowiazanie.Value >= 0
                    ? doRozliczeniaNaleznosc.Value - doRozliczeniaZobowiazanie.Value
                    : -(doRozliczeniaNaleznosc.Value - doRozliczeniaZobowiazanie.Value), _elemOpisuAnalitycznego.Kwota.Symbol);

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

        [Caption("Typ")]
        public TypRozrachunku? DokumentTyp
        {
            get
            {
                if (_elemOpisuAnalitycznego.Zrodlo is Platnosc pla)
                {
                    return pla.Typ;
                }
                else if(_elemOpisuAnalitycznego.Zrodlo is Zaplata zap)
                {
                    return zap.Typ;
                }
                return null; 
            }
        }
    }
}
