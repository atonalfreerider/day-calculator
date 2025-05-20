namespace day_calculator;

public static class Airports
{
    // Add airport codes and mapping for major European cities and the US
    public static readonly string[] AirportCodes =
    [
        // Poland
        "WAW", "KRK",
        // UK
        "LHR",
        // France
        "CDG",
        // Germany
        "FRA", "MUC", "BER",
        // Switzerland
        "ZRH", "GVA", "BSL",
        // Iceland
        "KEF",
        // Czech Republic
        "PRG",
        // US (major cities)
        "JFK", "EWR", "LGA", "BOS", "ORD", "LAX", "SFO", "SEA", "MIA", "DFW", "DEN", "IAD", "DCA", "ATL", "PHL", "DTW",
        "MSP", "CLT", "PHX", "IAH", "SAN", "TPA", "FLL", "BWI", "MDW", "SJC", "AUS", "DAL", "HOU", "OAK", "MCO", "SLC",
        "PDX", "STL", "CLE", "CMH", "CVG", "PIT", "MKE", "SMF", "RDU", "MCI", "BNA", "MSY", "SAT", "IND", "JAX", "OKC",
        "OMA", "RIC", "SDF", "TUL", "ABQ", "ELP", "ONT", "BUR", "LGB", "BOI", "TUS", "ANC",
    ];

    public static readonly Dictionary<string, string> AirportToZone = new()
    {
        // Poland
        ["WAW"] = "SCHENGEN", ["KRK"] = "SCHENGEN", ["KTW"] = "SCHENGEN", ["GDN"] = "SCHENGEN", ["POZ"] = "SCHENGEN",
        ["SZZ"] = "SCHENGEN", ["WRO"] = "SCHENGEN", ["RZE"] = "SCHENGEN", ["LCJ"] = "SCHENGEN", ["LUZ"] = "SCHENGEN",
        // UK
        ["LHR"] = "UK", ["LGW"] = "UK", ["LTN"] = "UK", ["STN"] = "UK", ["LCY"] = "UK", ["MAN"] = "UK", ["EDI"] = "UK",
        ["GLA"] = "UK", ["BRS"] = "UK", ["BHX"] = "UK",
        // France
        ["CDG"] = "SCHENGEN", ["ORY"] = "SCHENGEN", ["LYS"] = "SCHENGEN", ["NCE"] = "SCHENGEN", ["MRS"] = "SCHENGEN",
        ["BOD"] = "SCHENGEN", ["TLS"] = "SCHENGEN",
        // Germany
        ["FRA"] = "SCHENGEN", ["MUC"] = "SCHENGEN", ["TXL"] = "SCHENGEN", ["BER"] = "SCHENGEN", ["HAM"] = "SCHENGEN",
        ["DUS"] = "SCHENGEN", ["CGN"] = "SCHENGEN", ["STR"] = "SCHENGEN",
        // Netherlands
        ["AMS"] = "SCHENGEN", ["EIN"] = "SCHENGEN", ["RTM"] = "SCHENGEN",
        // Austria
        ["VIE"] = "SCHENGEN", ["SZG"] = "SCHENGEN",
        // Switzerland
        ["ZRH"] = "SCHENGEN", ["GVA"] = "SCHENGEN", ["BSL"] = "SCHENGEN",
        // Spain
        ["MAD"] = "SCHENGEN", ["BCN"] = "SCHENGEN", ["AGP"] = "SCHENGEN", ["PMI"] = "SCHENGEN", ["ALC"] = "SCHENGEN",
        ["VLC"] = "SCHENGEN",
        // Italy
        ["FCO"] = "SCHENGEN", ["MXP"] = "SCHENGEN", ["LIN"] = "SCHENGEN", ["VCE"] = "SCHENGEN", ["NAP"] = "SCHENGEN",
        ["BLQ"] = "SCHENGEN",
        // Belgium
        ["BRU"] = "SCHENGEN", ["CRL"] = "SCHENGEN",
        // Denmark
        ["CPH"] = "SCHENGEN",
        // Sweden
        ["ARN"] = "SCHENGEN", ["GOT"] = "SCHENGEN",
        // Norway
        ["OSL"] = "SCHENGEN",
        // Finland
        ["HEL"] = "SCHENGEN",
        // Portugal
        ["LIS"] = "SCHENGEN", ["OPO"] = "SCHENGEN", ["FAO"] = "SCHENGEN",
        // Iceland
        ["KEF"] = "SCHENGEN",
        // Czech Republic
        ["PRG"] = "SCHENGEN",
        // Hungary
        ["BUD"] = "SCHENGEN",
        // Slovakia
        ["BTS"] = "SCHENGEN",
        // Slovenia
        ["LJU"] = "SCHENGEN",
        // Luxembourg
        ["LUX"] = "SCHENGEN",
        // Estonia
        ["TLL"] = "SCHENGEN",
        // Latvia
        ["RIX"] = "SCHENGEN",
        // Lithuania
        ["VNO"] = "SCHENGEN",
        // Malta
        ["MLA"] = "SCHENGEN",
        // US (major cities)
        ["JFK"] = "OUTSIDE", ["EWR"] = "OUTSIDE", ["LGA"] = "OUTSIDE", ["BOS"] = "OUTSIDE", ["ORD"] = "OUTSIDE",
        ["LAX"] = "OUTSIDE", ["SFO"] = "OUTSIDE", ["SEA"] = "OUTSIDE", ["MIA"] = "OUTSIDE", ["DFW"] = "OUTSIDE",
        ["DEN"] = "OUTSIDE", ["IAD"] = "OUTSIDE", ["DCA"] = "OUTSIDE", ["ATL"] = "OUTSIDE", ["PHL"] = "OUTSIDE",
        ["DTW"] = "OUTSIDE", ["MSP"] = "OUTSIDE", ["CLT"] = "OUTSIDE", ["PHX"] = "OUTSIDE", ["IAH"] = "OUTSIDE",
        ["SAN"] = "OUTSIDE", ["TPA"] = "OUTSIDE", ["FLL"] = "OUTSIDE", ["BWI"] = "OUTSIDE", ["MDW"] = "OUTSIDE",
        ["SJC"] = "OUTSIDE", ["AUS"] = "OUTSIDE", ["DAL"] = "OUTSIDE", ["HOU"] = "OUTSIDE", ["OAK"] = "OUTSIDE",
        ["MCO"] = "OUTSIDE", ["SLC"] = "OUTSIDE", ["PDX"] = "OUTSIDE", ["STL"] = "OUTSIDE", ["CLE"] = "OUTSIDE",
        ["CMH"] = "OUTSIDE", ["CVG"] = "OUTSIDE", ["PIT"] = "OUTSIDE", ["MKE"] = "OUTSIDE", ["SMF"] = "OUTSIDE",
        ["RDU"] = "OUTSIDE", ["MCI"] = "OUTSIDE", ["BNA"] = "OUTSIDE", ["MSY"] = "OUTSIDE", ["SAT"] = "OUTSIDE",
        ["IND"] = "OUTSIDE", ["JAX"] = "OUTSIDE", ["OKC"] = "OUTSIDE", ["OMA"] = "OUTSIDE", ["RIC"] = "OUTSIDE",
        ["SDF"] = "OUTSIDE", ["TUL"] = "OUTSIDE", ["ABQ"] = "OUTSIDE", ["ELP"] = "OUTSIDE", ["ONT"] = "OUTSIDE",
        ["BUR"] = "OUTSIDE", ["LGB"] = "OUTSIDE", ["BOI"] = "OUTSIDE", ["TUS"] = "OUTSIDE", ["ANC"] = "OUTSIDE",
        // Ukraine
        ["IEV"] = "UKRAINE", ["KBP"] = "UKRAINE", ["ODS"] = "UKRAINE", ["LWO"] = "UKRAINE", ["HRK"] = "UKRAINE"
    };
}