using System;

namespace EDIVE.GeoToolkit
{
    public enum CoordRefSystem
    {
        Unknown,
        CRS_84,
        EPSG_3857,
        EPSG_4326,
        EPSG_5514,
        EPSG_3034,
        EPSG_3035,
        EPSG_3045,
        EPSG_3046,
        EPSG_3835,
        EPSG_3836,
        EPSG_4258,
        EPSG_5221,
        EPSG_32633,
        EPSG_32634,
        EPSG_102066, 
    }

    public static class CoordRefSystemUtility
    {
        public static readonly string WKT_EPSG_3857 = @"PROJCS[""WGS 84 / Pseudo-Mercator"",GEOGCS[""WGS 84"",DATUM[""WGS_1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4326""]],PROJECTION[""Mercator_1SP""],PARAMETER[""central_meridian"",0],PARAMETER[""scale_factor"",1],PARAMETER[""false_easting"",0],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AXIS[""X"",EAST],AXIS[""y"",NORTH],EXTENSION[""PROJ4"",""+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs""],AUTHORITY[""EPSG"",""3857""]]";
        public static readonly string WKT_EPSG_4326 = @"GEOGCS[""WGS 84"",DATUM[""WGS_1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4326""]]";
        public static readonly string WKT_EPSG_5514 = @"PROJCS[""S-JTSK / Krovak East North"",GEOGCS[""S-JTSK"",DATUM[""System_Jednotne_Trigonometricke_Site_Katastralni"",SPHEROID[""Bessel 1841"",6377397.155,299.1528128,AUTHORITY[""EPSG"",""7004""]],TOWGS84[589,76,480,0,0,0,0],AUTHORITY[""EPSG"",""6156""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4156""]],PROJECTION[""Krovak""],PARAMETER[""latitude_of_center"",49.5],PARAMETER[""longitude_of_center"",24.83333333333333],PARAMETER[""azimuth"",30.28813972222222],PARAMETER[""pseudo_standard_parallel_1"",78.5],PARAMETER[""scale_factor"",0.9999],PARAMETER[""false_easting"",0],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AXIS[""X"",EAST],AXIS[""y"",NORTH],AUTHORITY[""EPSG"",""5514""]]";
        public static readonly string WKT_EPSG_3034 = @"PROJCS[""ETRS89 / LCC Europe"",GEOGCS[""ETRS89"",DATUM[""European_Terrestrial_Reference_System_1989"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[""EPSG"",""6258""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4258""]],PROJECTION[""Lambert_Conformal_Conic_2SP""],PARAMETER[""standard_parallel_1"",35],PARAMETER[""standard_parallel_2"",65],PARAMETER[""latitude_of_origin"",52],PARAMETER[""central_meridian"",10],PARAMETER[""false_easting"",4000000],PARAMETER[""false_northing"",2800000],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AUTHORITY[""EPSG"",""3034""]]";
        public static readonly string WKT_EPSG_3035 = @"PROJCS[""ETRS89 / LAEA Europe"",GEOGCS[""ETRS89"",DATUM[""European_Terrestrial_Reference_System_1989"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[""EPSG"",""6258""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4258""]],PROJECTION[""Lambert_Azimuthal_Equal_Area""],PARAMETER[""latitude_of_center"",52],PARAMETER[""longitude_of_center"",10],PARAMETER[""false_easting"",4321000],PARAMETER[""false_northing"",3210000],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AUTHORITY[""EPSG"",""3035""]]";
        public static readonly string WKT_EPSG_3045 = @"PROJCS[""ETRS89 / UTM zone 33N (N-E)"",GEOGCS[""ETRS89"",DATUM[""European_Terrestrial_Reference_System_1989"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[""EPSG"",""6258""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4258""]],PROJECTION[""Transverse_Mercator""],PARAMETER[""latitude_of_origin"",0],PARAMETER[""central_meridian"",15],PARAMETER[""scale_factor"",0.9996],PARAMETER[""false_easting"",500000],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AUTHORITY[""EPSG"",""3045""]]";
        public static readonly string WKT_EPSG_3046 = @"PROJCS[""ETRS89 / UTM zone 34N (N-E)"",GEOGCS[""ETRS89"",DATUM[""European_Terrestrial_Reference_System_1989"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[""EPSG"",""6258""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4258""]],PROJECTION[""Transverse_Mercator""],PARAMETER[""latitude_of_origin"",0],PARAMETER[""central_meridian"",21],PARAMETER[""scale_factor"",0.9996],PARAMETER[""false_easting"",500000],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AUTHORITY[""EPSG"",""3046""]]";
        public static readonly string WKT_EPSG_3835 = @"PROJCS[""Pulkovo 1942(83) / Gauss-Kruger zone 3"",GEOGCS[""Pulkovo 1942(83)"",DATUM[""Pulkovo_1942_83"",SPHEROID[""Krassowsky 1940"",6378245,298.3,AUTHORITY[""EPSG"",""7024""]],TOWGS84[26,-121,-78,0,0,0,0],AUTHORITY[""EPSG"",""6178""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4178""]],PROJECTION[""Transverse_Mercator""],PARAMETER[""latitude_of_origin"",0],PARAMETER[""central_meridian"",15],PARAMETER[""scale_factor"",1],PARAMETER[""false_easting"",3500000],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AUTHORITY[""EPSG"",""3835""]]";
        public static readonly string WKT_EPSG_3836 = @"PROJCS[""Pulkovo 1942(83) / Gauss-Kruger zone 4"",GEOGCS[""Pulkovo 1942(83)"",DATUM[""Pulkovo_1942_83"",SPHEROID[""Krassowsky 1940"",6378245,298.3,AUTHORITY[""EPSG"",""7024""]],TOWGS84[26,-121,-78,0,0,0,0],AUTHORITY[""EPSG"",""6178""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4178""]],PROJECTION[""Transverse_Mercator""],PARAMETER[""latitude_of_origin"",0],PARAMETER[""central_meridian"",21],PARAMETER[""scale_factor"",1],PARAMETER[""false_easting"",4500000],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AUTHORITY[""EPSG"",""3836""]]";
        public static readonly string WKT_EPSG_4258 = @"GEOGCS[""ETRS89"",DATUM[""European_Terrestrial_Reference_System_1989"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[""EPSG"",""6258""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4258""]]";
        public static readonly string WKT_EPSG_5221 = @"PROJCS[""S-JTSK (Ferro) / Krovak East North"",GEOGCS[""S-JTSK (Ferro)"",DATUM[""System_Jednotne_Trigonometricke_Site_Katastralni_Ferro"",SPHEROID[""Bessel 1841"",6377397.155,299.1528128,AUTHORITY[""EPSG"",""7004""]],TOWGS84[589,76,480,0,0,0,0],AUTHORITY[""EPSG"",""6818""]],PRIMEM[""Ferro"",-17.66666666666667,AUTHORITY[""EPSG"",""8909""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4818""]],PROJECTION[""Krovak""],PARAMETER[""latitude_of_center"",49.5],PARAMETER[""longitude_of_center"",42.5],PARAMETER[""azimuth"",30.28813972222222],PARAMETER[""pseudo_standard_parallel_1"",78.5],PARAMETER[""scale_factor"",0.9999],PARAMETER[""false_easting"",0],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AXIS[""X"",EAST],AXIS[""y"",NORTH],AUTHORITY[""EPSG"",""5221""]]";
        public static readonly string WKT_EPSG_32633 = @"PROJCS[""WGS 84 / UTM zone 33N"",GEOGCS[""WGS 84"",DATUM[""WGS_1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4326""]],PROJECTION[""Transverse_Mercator""],PARAMETER[""latitude_of_origin"",0],PARAMETER[""central_meridian"",15],PARAMETER[""scale_factor"",0.9996],PARAMETER[""false_easting"",500000],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AXIS[""Easting"",EAST],AXIS[""Northing"",NORTH],AUTHORITY[""EPSG"",""32633""]]";
        public static readonly string WKT_EPSG_32634 = @"PROJCS[""WGS 84 / UTM zone 34N"",GEOGCS[""WGS 84"",DATUM[""WGS_1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4326""]],PROJECTION[""Transverse_Mercator""],PARAMETER[""latitude_of_origin"",0],PARAMETER[""central_meridian"",21],PARAMETER[""scale_factor"",0.9996],PARAMETER[""false_easting"",500000],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AXIS[""Easting"",EAST],AXIS[""Northing"",NORTH],AUTHORITY[""EPSG"",""32634""]]";
        public static readonly string WKT_EPSG_102066 = @"PROJCS[""S-JTSK_Ferro_Krovak_East_North"",GEOGCS[""GCS_S_JTSK_Ferro"",DATUM[""Jednotne_Trigonometricke_Site_Katastralni"",SPHEROID[""Bessel_1841"",6377397.155,299.1528128]],PRIMEM[""Ferro"",-17.66666666666667],UNIT[""Degree"",0.017453292519943295]],PROJECTION[""Krovak""],PARAMETER[""False_Easting"",0],PARAMETER[""False_Northing"",0],PARAMETER[""Pseudo_Standard_Parallel_1"",78.5],PARAMETER[""Scale_Factor"",0.9999],PARAMETER[""Azimuth"",30.28813975277778],PARAMETER[""Longitude_Of_Center"",42.5],PARAMETER[""Latitude_Of_Center"",49.5],PARAMETER[""X_Scale"",-1],PARAMETER[""Y_Scale"",1],PARAMETER[""XY_Plane_Rotation"",90],UNIT[""Meter"",1],AUTHORITY[""EPSG"",""102066""]]";

        public static string ToWellKnownText(this CoordRefSystem coordRefSystem)
        {
            return coordRefSystem switch
            {
                CoordRefSystem.CRS_84 => WKT_EPSG_4326,
                CoordRefSystem.EPSG_3857 => WKT_EPSG_3857,
                CoordRefSystem.EPSG_4326 => WKT_EPSG_4326,
                CoordRefSystem.EPSG_5514 => WKT_EPSG_5514,
                CoordRefSystem.EPSG_3034 => WKT_EPSG_3034,
                CoordRefSystem.EPSG_3035 => WKT_EPSG_3035,
                CoordRefSystem.EPSG_3045 => WKT_EPSG_3045,
                CoordRefSystem.EPSG_3046 => WKT_EPSG_3046,
                CoordRefSystem.EPSG_3835 => WKT_EPSG_3835,
                CoordRefSystem.EPSG_3836 => WKT_EPSG_3836,
                CoordRefSystem.EPSG_4258 => WKT_EPSG_4258,
                CoordRefSystem.EPSG_5221 => WKT_EPSG_5221,
                CoordRefSystem.EPSG_32633 => WKT_EPSG_32633,
                CoordRefSystem.EPSG_32634 => WKT_EPSG_32634,
                CoordRefSystem.EPSG_102066 => WKT_EPSG_102066,
                CoordRefSystem.Unknown => null,
                _ => throw new ArgumentOutOfRangeException(nameof(coordRefSystem), coordRefSystem, null)
            };
        }

        public static bool TryParseString(string name, out CoordRefSystem result)
        {
            result = name switch
            {
                "CRS:84" => CoordRefSystem.CRS_84,
                "EPSG:3857" => CoordRefSystem.EPSG_3857,
                "EPSG:4326" => CoordRefSystem.EPSG_4326,
                "EPSG:5514" => CoordRefSystem.EPSG_5514,
                "EPSG:3034" => CoordRefSystem.EPSG_3034,
                "EPSG:3035" => CoordRefSystem.EPSG_3035,
                "EPSG:3045" => CoordRefSystem.EPSG_3045,
                "EPSG:3046" => CoordRefSystem.EPSG_3046,
                "EPSG:3835" => CoordRefSystem.EPSG_3835,
                "EPSG:3836" => CoordRefSystem.EPSG_3836,
                "EPSG:4258" => CoordRefSystem.EPSG_4258,
                "EPSG:5221" => CoordRefSystem.EPSG_5221,
                "EPSG:32633" => CoordRefSystem.EPSG_32633,
                "EPSG:32634" => CoordRefSystem.EPSG_32634,
                "EPSG:102066" => CoordRefSystem.EPSG_102066,
                _ => CoordRefSystem.Unknown
            };
            
            return result != CoordRefSystem.Unknown;
        }
    }
}
