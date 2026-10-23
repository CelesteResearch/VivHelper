using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Mods.VivHelper.Mathematics.Curves;
using Celeste.Mods.VivHelper.Runtime.Curves;

namespace Celeste.Mods.VivHelper.Entities.Curves
{
    [SceneTracked(true)]
    [RuntimeTypeAlias("VivHelper.Entities.CurveEntity")]
    [CustomEntity("VivHelper/CurveEntity")]
    public class CurveEntity : SceneEntityState
    {
        public BezierSystem bezier;

        public int pointsLength;

        public bool spline;

        public string identifier;

        public int tStart;

        public int tEnd;

        private readonly VivCurveContext context;

        public CurveEntity(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.NodesWithPosition(offset)[0])
        {
            Depth = 1000000;
            context = VivCurveContext.For(engine);
            string text = data.Attr("CurvesNumberOfPoints", "Automatic");
            spline = data.Bool("Spline", false);
            identifier = data.Attr("Identifier", "").Trim();
            Vector2[] array = data.NodesWithPosition(offset);
            pointsLength = array.Length;
            if (pointsLength < 3)
            {
                throw new Exception("Not enough points to produce a curve.");
            }

            if (pointsLength == 3)
            {
                if (spline)
                {
                    throw new Exception("Not enough points to produce a spline.");
                }

                BezierObject[] b = new Bezier2[1]
                {
                    new Bezier2(array[0], array[1], array[2])
                };
                bezier = new BezierSystem(b);
                return;
            }

            if (pointsLength == 4)
            {
                if (spline)
                {
                    bezier = new BezierSystem(new List<BezierObject>
                    {
                        new Bezier2(array[0], array[1], array[2]),
                        new Bezier2(array[2], array[3], array[0])
                    }.ToArray());
                }
                else
                {
                    bezier = new BezierSystem(new List<BezierObject>
                    {
                        new Bezier3(array[0], array[1], array[2], array[3])
                    }.ToArray());
                }

                return;
            }

            string text2 = text;
            string text3 = text2;
            if (!(text3 == "All Simple"))
            {
                if (text3 == "All Cubic")
                {
                    if (pointsLength % 3 != ((!spline) ? 1 : 0))
                    {
                        throw new Exception(
                            "Number of Points do not add up to the necessary amount. If the following says \"true\" then you may want to disable Spline.\t" +
                            spline);
                    }

                    BezierObject[] array2 = new BezierObject[pointsLength / 3];
                    for (int i = 0; i < pointsLength; i += 3)
                    {
                        array2[i / 3] = new Bezier3(array[i], array[i + 1], array[i + 2],
                            array[(i + 3) % pointsLength]);
                    }

                    bezier = new BezierSystem(array2);
                }
                else
                {
                    if (text == "Automatic")
                    {
                        int num = pointsLength - ((!spline) ? 1 : 0);
                        List<char> list = new List<char>();
                        int num2 = 0;
                        while (num >= 2)
                        {
                            if (num == 3)
                            {
                                list.Add('3');
                            }
                            else
                            {
                                list.Add('2');
                            }

                            num -= 2;
                            num2++;
                        }

                        text = new string(list.ToArray());
                    }

                    char[] array3 = text.ToCharArray();
                    List<int> list2 = new List<int>();
                    char[] array4 = array3;
                    foreach (char c in array4)
                    {
                        list2.Add(int.Parse(c.ToString()));
                    }

                    List<BezierObject> list3 = new List<BezierObject>();
                    int num3 = 0;
                    foreach (int item in list2)
                    {
                        switch (item)
                        {
                            case 2:
                                list3.Add(new Bezier2(array[num3], array[num3 + 1], array[(num3 + 2) % pointsLength]));
                                break;
                            case 3:
                                list3.Add(new Bezier3(array[num3], array[num3 + 1], array[num3 + 2],
                                    array[(num3 + 3) % pointsLength]));
                                num3++;
                                break;
                            default:
                                throw new Exception("Invalid Variable: Check your CurvesNumberOfPoints variable.");
                        }

                        num3 += 2;
                    }

                    bezier = new BezierSystem(list3.ToArray());
                }
            }
            else
            {
                if (pointsLength % 2 == (spline ? 1 : 0))
                {
                    throw new Exception(
                        "Number of Points do not add up to the necessary amount. If the following says \"true\" then you may want to disable Spline.\t" +
                        spline);
                }

                List<BezierObject> list4 = new List<BezierObject>();
                for (int k = 0; k < pointsLength; k += 2)
                {
                    list4.Add(new Bezier2(array[k], array[k + 1], array[(k + 2) % pointsLength]));
                }

                bezier = new BezierSystem(list4.ToArray());
            }

            tStart = bezier.tStart;
            tEnd = bezier.tEnd;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (context.Curves.ContainsKey(identifier))
            {
                context.Curves.Remove(identifier);
            }

            context.Curves[identifier] = this;
        }

        public override void Removed(object scene)
        {
            context.Curves.Remove(identifier);
            base.Removed(scene);
        }

        public int getNumOfCurves()
        {
            return bezier.curves.Length;
        }
    }
}
