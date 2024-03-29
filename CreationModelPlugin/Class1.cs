using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Level level1 = GetLevel(doc, "Уровень 1");
            Level level2 = GetLevel(doc, "Уровень 2");



            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth  / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            Transaction transaction = new Transaction(doc, "Построекние стены");
            transaction.Start();



            List<Wall> walls = WallCreate(doc, level1, points, level2, width, depth);

            AddDoor(doc, level1, walls[0]);
            AddWindows(doc, level1, walls[1]);
            AddWindows(doc, level1, walls[2]);
            AddWindows(doc, level1, walls[3]);

            transaction.Commit();


            return Result.Succeeded;


        }

        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            if (!doorType.IsActive)
                doorType.Activate();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }
        private void AddWindows(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowsType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1220 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            if (!windowsType.IsActive)
                windowsType.Activate();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ pointOrigin = (point1 + point2) / 2;

            double pointX = ((point1 + point2) / 2).X;
            double pointY = ((point1 + point2) / 2).Y;
            double pointZ = ((point1 + point2) / 2).Z + UnitUtils.ConvertToInternalUnits(1500, UnitTypeId.Millimeters);

            XYZ point = new XYZ(pointX, pointY, pointZ);


            doc.Create.NewFamilyInstance(point, windowsType, wall, level1, StructuralType.NonStructural);
        }
        public static Level GetLevel(Document doc, string name)
        {

            List<Level> listLevel = new FilteredElementCollector(doc)
             .OfClass(typeof(Level))
             .OfType<Level>()
             .ToList();

            Level level = listLevel
                 .Where(x => x.Name.Equals(name))
                 .FirstOrDefault();

            return level;
        }
        public static List<Wall> WallCreate(Document doc, Level level1, List<XYZ> points, Level level2, double width, double length)
        {

            List<Wall> walls = new List<Wall>();

            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }

            return walls;
        }


    }
}
