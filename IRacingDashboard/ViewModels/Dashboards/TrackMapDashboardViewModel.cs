﻿using Adapters;
using HelixToolkit.Wpf;
using IRacingDashboard.Enums;
using IRacingDashboard.Models;
using IRacingDashboard.Services;
using irsdkSharp.Serialization.Models.Data;
using irsdkSharp.Serialization.Models.Fastest;
using irsdkSharp.Serialization.Models.Session;
using irsdkSharp.Serialization.Models.Session.SplitTimeInfo;
using irsdkSharp.Serialization.Models.Session.WeekendInfo;
using Newtonsoft.Json;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xml;

namespace IRacingDashboard.ViewModels
{
    public class TrackMapDashboardViewModel : BaseViewModel
    {
        private readonly TelemetryService _telemetryService;

        private double _ghostX = 0;
        private double _ghostZ = 0;
        private double _ghostHeading = 0; // 0 = facing up (Z+)
        private DateTime _lastUpdateTime = DateTime.UtcNow;
        private bool _isRecording = false;
        private int _startingLap = -1;
        string _trackName = "";
        private bool _recordedOneLap = false;
        public ObservableCollection<UIRenderedSector> SectorDisplay { get; set; } = new();



        public TrackMapDashboardViewModel()
        {
            _telemetryService = TelemetryService.Instance;
            _telemetryService.TelemetryUpdated += OnTelemetryUpdated;
            _telemetryService.WeekendInfoUpdated += _telemetryService_WeekendInfoUpdated;

            _telemetryService.SectorsChanged += _telemetryService_SectorsChanged; ; // ✅ Listen for connection changes
            _telemetryService.SplitTimeInfoChanged += _telemetryService_SplitTimeInfoChanged; ; ; // ✅ Listen for connection changes

            _telemetryService.Start();

            TryLoadTrack();

            
        }

        private void _telemetryService_SplitTimeInfoChanged(SplitTimeInfoModel obj)
        {

            if (obj?.Sectors == null || obj.Sectors.Count == 0)
                return;

            SplitSectorInfo = obj.Sectors
                .OrderBy(s => s.SectorNum)
                .ToList(); // replaces the old list and triggers OnPropertyChanged
        }

        private void _telemetryService_SectorsChanged(int numberOfSectors)
        {
            NrOfSectors= numberOfSectors;
        }

        private void _telemetryService_WeekendInfoUpdated(IRacingSessionModel sessionInfo)
        {
            if (!string.IsNullOrEmpty(_trackName)) return;

            _trackName = sessionInfo.WeekendInfo.TrackName;
           
            TryLoadTrack();
        }
       
        private bool TryLoadTrack()
        {
             _trackName = "limerock 2019 gp";

            if (string.IsNullOrEmpty(_trackName)) return false;

            string filename = $"{_trackName}.json";

            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                var loadedPoints = JsonConvert.DeserializeObject<ObservableCollection<TrackPoint>>(json);

                if (loadedPoints != null && loadedPoints.Count > 0)
                {
                    RecordedPoints = loadedPoints; // 🔥 triggers PropertyChanged now
                    _recordedOneLap = true;
                    DrawRecordedTrack();
                    CreateCar();
                    return true;
                }
            }

            return false;
        }

        private void StartRecording()
        {
            RecordedPoints.Clear();
            _ghostX = 0;
            _ghostZ = 0;
            _ghostHeading = 0;
            _lastUpdateTime = DateTime.UtcNow;
            _isRecording = true;
        }

        private void StopRecording()
        {
            _isRecording = false;
            _recordedOneLap = true;
            DrawRecordedTrack();
            SaveToJson();

        }

       

        private void SaveToJson()
        {
      
            var json = JsonConvert.SerializeObject(RecordedPoints, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_trackName+".json", json);
        }



        private double _lastLapDistPct = -1;

        private void OnTelemetryUpdated(Data telemetry)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                double steering = telemetry.SteeringWheelAngle;
                double yaw = telemetry.Yaw;
                double yawnorth = telemetry.YawNorth;
                double yawrate = telemetry.YawRate;
                double yawrate_St = telemetry.YawRate_ST;

                this.LapPct = telemetry.LapDistPct;                
                OnPropertyChanged(nameof(LapPct));

                //Check Sectors

                CheckSectorsTimes(telemetry);
                

                int currentLap = telemetry.Lap;

                if (_startingLap == -1)
                {
                    _startingLap = currentLap;
                    _lastLapDistPct = LapPct;
                    return;
                }

                if (!_isRecording && !_recordedOneLap && currentLap > _startingLap)
                {
                    StartRecording();
                }

                if (_isRecording && currentLap > _startingLap + 1)
                {
                    StopRecording();
                }

                if (_isRecording)
                {
                    double deltaLapDist = LapPct - _lastLapDistPct;
                    _lastLapDistPct = LapPct;

                    if (deltaLapDist < 0) return; // Skip if LapDistPct reset


                    if (RecordedPoints.Count == 0 ||
                        GetDistance(RecordedPoints[^1], _ghostX, _ghostZ) > 2)
                    {
                        RecordedPoints.Add(new TrackPoint { X = LapPct, Z = yawrate_St });
                    }
                }
            });
        }




        private int _lastSectorIndex = -1;
        private double _sectorStartTime = 0;

        private List<double> _bestSectorTimes = new(); // best overall (session best)
        private List<double> _lastLapSectorTimes = new(); // this lap
        public ObservableCollection<SectorState> SectorStates { get; set; } = new();
        private int _lastLapNumber;
        private double _lastLapTime = 0;
        private double _lastLapPct = 0;
        private double _sectorStartDelta = 0;
        private List<double> _sectorDeltas = new();

        private void CheckSectorsTimes(Data telemetry)
        {
            double lapPct = telemetry.LapDistPct;
            double lapDelta = telemetry.LapDeltaToSessionBestLap;

            if (SplitSectorInfo == null || SplitSectorInfo.Count == 0 || NrOfSectors == 0)
                return;

            int currentSectorIndex = GetCurrentSectorIndex(lapPct);

            // 🔁 Detect new lap and finalize last sector before resetting
            if (_lastLapNumber != -1 && telemetry.Lap != _lastLapNumber)
            {
                int completedLapNumber = _lastLapNumber;
                _lastLapNumber = telemetry.Lap; // Update immediately to prevent retrigger
                int lastSectorIndex = NrOfSectors - 1;
                double accumulatedDelta = _sectorDeltas.Take(lastSectorIndex).Sum();

                Task.Run(async () =>
                {
                    await Task.Delay(1000); // ⏱ Wait for LapLastLapTime to become valid

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        double totalLapTime = telemetry.LapLastLapTime;
                        double bestLapTime = telemetry.LapBestLapTime;
                        double totalDelta = totalLapTime - bestLapTime;
                        double finalDelta = totalDelta - accumulatedDelta;

                        while (_sectorDeltas.Count <= lastSectorIndex)
                            _sectorDeltas.Add(0);
                        _sectorDeltas[lastSectorIndex] = finalDelta;

                        SectorState state = finalDelta < 0 ? SectorState.SessionBest :
                                            finalDelta == 0 ? SectorState.PersonalBest :
                                            SectorState.Slower;

                        while (SectorStates.Count <= lastSectorIndex)
                            SectorStates.Add(SectorState.Clear);
                        SectorStates[lastSectorIndex] = state;

                        if (SectorDisplay.Count <= lastSectorIndex)
                        {
                            SectorDisplay.Add(new UIRenderedSector
                            {
                                SectorNumber = lastSectorIndex + 1,
                                DeltaToBest = finalDelta,
                                State = state
                            });
                        }
                        else
                        {
                            SectorDisplay[lastSectorIndex].DeltaToBest = finalDelta;
                            SectorDisplay[lastSectorIndex].State = state;
                        }

                        OnPropertyChanged(nameof(SectorDisplay));
                        OnPropertyChanged(nameof(SectorStates));
                        UpdateSectorOverlay();

                        // ⏱ Clear after another 2 seconds
                        Task.Run(async () =>
                        {
                            await Task.Delay(2000);
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                SectorDisplay.Clear();
                                SectorStates.Clear();
                                for (int i = 0; i < NrOfSectors; i++)
                                    SectorStates.Add(SectorState.Clear);

                                UpdateSectorOverlay();
                                OnPropertyChanged(nameof(SectorDisplay));
                                OnPropertyChanged(nameof(SectorStates));
                            });
                        });

                        _lastSectorIndex = -1;
                        _sectorStartDelta = 0;
                        _sectorDeltas.Clear();
                    });
                });
            }




            // Regular sector change handling
            if (currentSectorIndex > _lastSectorIndex)
            {
                if (currentSectorIndex == 1)
                    _sectorStartDelta = 0;

                int prevSector = _lastSectorIndex;
                if (prevSector >= 0)
                {
                    double sectorDelta = lapDelta - _sectorStartDelta;

                    while (_sectorDeltas.Count <= prevSector)
                        _sectorDeltas.Add(0);
                    _sectorDeltas[prevSector] = sectorDelta;

                    SectorState state = sectorDelta < 0 ? SectorState.SessionBest :
                                        sectorDelta == 0 ? SectorState.PersonalBest :
                                        SectorState.Slower;

                    while (SectorStates.Count <= prevSector)
                        SectorStates.Add(SectorState.Clear);
                    SectorStates[prevSector] = state;

                    if (SectorDisplay.Count <= prevSector)
                    {
                        SectorDisplay.Add(new UIRenderedSector
                        {
                            SectorNumber = prevSector + 1,
                            DeltaToBest = sectorDelta,
                            State = state
                        });
                    }
                    else
                    {
                        SectorDisplay[prevSector].DeltaToBest = sectorDelta;
                        SectorDisplay[prevSector].State = state;
                    }

                    OnPropertyChanged(nameof(SectorDisplay));
                    OnPropertyChanged(nameof(SectorStates));
                    UpdateSectorOverlay();
                }

                _sectorStartDelta = lapDelta;
                _lastSectorIndex = currentSectorIndex;
            }
        }







        private int GetCurrentSectorIndex(double lapPct)
        {
            for (int i = SplitSectorInfo.Count - 1; i >= 0; i--)
            {
                if (lapPct >= SplitSectorInfo[i].SectorStartPct)
                    return i;
            }
            return 0;
        }


        private double GetDistance(TrackPoint last, double x, double z)
        {
            double dx = last.X - x;
            double dz = last.Z - z;
            return Math.Sqrt(dx * dx + dz * dz);
        }


        public Model3D CarModel { get; set; }
        public Point3D CarPosition { get; set; }
        public Model3D StartLineModel { get; set; }
        private List<Point3D> _leftEdge;
        private List<Point3D> _rightEdge;
        private double _overlayZ;

        private List<Point> scaled2D = new();

    
        private void DrawRecordedTrack()
        {
            if (RecordedPoints == null || RecordedPoints.Count < 3) return;

            const double TrackLength = 5000;
            const double trackWidth = 8.0;
            const double wallHeight = 2.0;
            const double roadSurfaceZ = 0.1;
            double wallBaseZ = -5.0;    // <-- NEW bottom

            var pointList = RecordedPoints.ToList();

            // Reconstruct centerline from yaw deltas
            double x = 0, y = 0;
            var path = new List<Point> { new Point(x, y) };

            for (int i = 1; i < pointList.Count; i++)
            {
                double pctDelta = pointList[i].X - pointList[i - 1].X;
                if (pctDelta < 0) continue;

                double distance = pctDelta * TrackLength;
                double yawRad = pointList[i].Z;

                x += distance * Math.Cos(yawRad);
                y += distance * Math.Sin(yawRad);

                path.Add(new Point(x, y));
            }

            if (path.Count < 3) return;

            // Center and scale
            double minX = path.Min(p => p.X);
            double maxX = path.Max(p => p.X);
            double minY = path.Min(p => p.Y);
            double maxY = path.Max(p => p.Y);
            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;
            double scale = 200 / Math.Max(maxX - minX, maxY - minY);

            scaled2D = path.Select(p =>
                new Point((p.X - centerX) * scale, (p.Y - centerY) * scale)).ToList();

            if (!scaled2D.First().Equals(scaled2D.Last()))
                scaled2D.Add(scaled2D[0]);

            // Compute left/right edges from centerline
            var leftEdge = new List<Point3D>();
            var rightEdge = new List<Point3D>();
            double halfWidth = trackWidth / 2.0;

            for (int i = 0; i < scaled2D.Count; i++)
            {
                var current = scaled2D[i];
                Point prev = i > 0 ? scaled2D[i - 1] : scaled2D[i];
                Point next = i < scaled2D.Count - 1 ? scaled2D[i + 1] : scaled2D[i];

                Vector dir = next - prev;
                dir.Normalize();

                Vector perp = new Vector(-dir.Y, dir.X);
                perp.Normalize();

                var left = new Point(current.X + perp.X * halfWidth, current.Y + perp.Y * halfWidth);
                var right = new Point(current.X - perp.X * halfWidth, current.Y - perp.Y * halfWidth);

                leftEdge.Add(new Point3D(left.X, left.Y, 0));
                rightEdge.Add(new Point3D(right.X, right.Y, 0));
            }

            if (leftEdge.Count != rightEdge.Count)
                throw new InvalidOperationException("Left/right edge point count mismatch");

            // Build mesh
            var meshBuilder = new MeshBuilder(false, false);

            // Side walls (extruded up)
            // Left wall (from Z = -2 up to 0)
            for (int i = 0; i < leftEdge.Count - 1; i++)
            {
                var p0 = new Point3D(leftEdge[i].X, leftEdge[i].Y, wallBaseZ);
                var p1 = new Point3D(leftEdge[i + 1].X, leftEdge[i + 1].Y, wallBaseZ);
                var p2 = leftEdge[i + 1]; // Z = 0
                var p3 = leftEdge[i];     // Z = 0
                meshBuilder.AddQuad(p0, p1, p2, p3);
            }

            // Right wall (from Z = -2 up to 0)
            for (int i = 0; i < rightEdge.Count - 1; i++)
            {
                var p0 = new Point3D(rightEdge[i].X, rightEdge[i].Y, wallBaseZ);
                var p1 = new Point3D(rightEdge[i + 1].X, rightEdge[i + 1].Y, wallBaseZ);
                var p2 = rightEdge[i + 1]; // Z = 0
                var p3 = rightEdge[i];     // Z = 0
                meshBuilder.AddQuad(p0, p1, p2, p3);
            }


            // Road surface
            for (int i = 0; i < leftEdge.Count - 1; i++)
            {
                var p0 = new Point3D(leftEdge[i].X, leftEdge[i].Y, roadSurfaceZ);
                var p1 = new Point3D(rightEdge[i].X, rightEdge[i].Y, roadSurfaceZ);
                var p2 = new Point3D(rightEdge[i + 1].X, rightEdge[i + 1].Y, roadSurfaceZ);
                var p3 = new Point3D(leftEdge[i + 1].X, leftEdge[i + 1].Y, roadSurfaceZ);
                meshBuilder.AddQuad(p0, p1, p2, p3);
            }

            TrackMesh = meshBuilder.ToMesh();



            // Generate the start line
            if (scaled2D.Count > 1)
            {
                var p0 = scaled2D[0];
                var p1 = scaled2D[1];

                Vector dir = p1 - p0;
                dir.Normalize();
                Vector perp = new Vector(-dir.Y, dir.X); // perpendicular to track direction
                perp.Normalize();

                double lineLength = 20.0;    // total width across track
                double lineThickness = 0.5;  // visual depth
                double lineZ = roadSurfaceZ + 0.5; // float just above the track

                Point center = p0;
                var a = new Point(center.X + perp.X * lineLength / 2, center.Y + perp.Y * lineLength / 2);
                var b = new Point(center.X - perp.X * lineLength / 2, center.Y - perp.Y * lineLength / 2);

                Vector thicknessVec = dir * 2; // thickness of the line (along direction)

                var startLineBuilder = new MeshBuilder();
                startLineBuilder.AddQuad(
                    new Point3D(a.X - thicknessVec.X, a.Y - thicknessVec.Y, lineZ),
                    new Point3D(b.X - thicknessVec.X, b.Y - thicknessVec.Y, lineZ),
                    new Point3D(b.X + thicknessVec.X, b.Y + thicknessVec.Y, lineZ),
                    new Point3D(a.X + thicknessVec.X, a.Y + thicknessVec.Y, lineZ));

                StartLineModel = new GeometryModel3D
                {
                    Geometry = startLineBuilder.ToMesh(),
                    Material = new DiffuseMaterial(Brushes.White)
                };

                OnPropertyChanged(nameof(StartLineModel));
            }





            // 🔁 Save edges for reuse
            _leftEdge = leftEdge;
            _rightEdge = rightEdge;
            _overlayZ = roadSurfaceZ + 0.02;


            UpdateSectorOverlay();


        }

        public void UpdateSectorOverlay()
        {
            if (_leftEdge == null || _rightEdge == null || _leftEdge.Count != _rightEdge.Count)
                return;
            if (SplitSectorInfo == null || SplitSectorInfo.Count == 0)
                return;

            var sectorGroup = new Model3DGroup();
            int totalPoints = _leftEdge.Count - 1;

            for (int i = 0; i < SplitSectorInfo.Count; i++)
            {
                double startPct = SplitSectorInfo[i].SectorStartPct;
                double endPct = (i < SplitSectorInfo.Count - 1)
                                ? SplitSectorInfo[i + 1].SectorStartPct
                                : 1.0;

                int startIndex = (int)(startPct * totalPoints);
                int endIndex = (int)(endPct * totalPoints);

                if (endIndex > totalPoints) endIndex = totalPoints;

                SectorState state = (i < SectorStates.Count) ? SectorStates[i] : SectorState.Clear;

                var sector = CreateSectorModel(_leftEdge, _rightEdge, startIndex, endIndex, _overlayZ, state);
                if (sector != null)
                    sectorGroup.Children.Add(sector);
            }

            SectorOverlayModel = sectorGroup;
            OnPropertyChanged(nameof(SectorOverlayModel));
        }



        private GeometryModel3D CreateSectorModel(List<Point3D> left, List<Point3D> right,
                                                  int start, int end, double z, SectorState state)
        {
            var builder = new MeshBuilder();

            for (int i = start; i < end; i++)
            {
                var p0 = new Point3D(left[i].X, left[i].Y, z);
                var p1 = new Point3D(right[i].X, right[i].Y, z);
                var p2 = new Point3D(right[i + 1].X, right[i + 1].Y, z);
                var p3 = new Point3D(left[i + 1].X, left[i + 1].Y, z);

                builder.AddQuad(p0, p1, p2, p3);
            }

            Brush color = state switch
            {
                SectorState.Slower => Brushes.Goldenrod,
                SectorState.PersonalBest => Brushes.LimeGreen,
                SectorState.SessionBest => Brushes.MediumPurple,
                SectorState.Clear => Brushes.Transparent,
                _ => Brushes.Gray
            };

            return new GeometryModel3D
            {
                Geometry = builder.ToMesh(),
                Material = MaterialHelper.CreateMaterial(color),
                BackMaterial = MaterialHelper.CreateMaterial(color)
            };
        }





        private void UpdateCarPosition(double lapDistPct)
        {
            if (scaled2D == null || scaled2D.Count < 2) return;

            double total = scaled2D.Count - 1;
            double exactIndex = lapDistPct * total;
            int index = (int)Math.Floor(exactIndex);
            double t = exactIndex - index;

            if (index < 0 || index >= scaled2D.Count - 1) return;

            Point p1 = scaled2D[index];
            Point p2 = scaled2D[index + 1];

            double x = p1.X + (p2.X - p1.X) * t;
            double y = p1.Y + (p2.Y - p1.Y) * t;

            CarPosition = new Point3D(x, y, 1);
            CarLabelTextPosition = new Point3D(x, y + 2,17); // slightly above the sphere

            OnPropertyChanged(nameof(CarPosition));
        }

        private void CreateCar()
        {
            var builder = new MeshBuilder();
            

            // Create the position sphere
            var sphereBuilder = new MeshBuilder();
            sphereBuilder.AddSphere(new Point3D(0, 0, 2), 5, 16, 16);
            CarModel = new GeometryModel3D
            {
                Geometry = sphereBuilder.ToMesh(),
                Material = new DiffuseMaterial(Brushes.White)
            };

            OnPropertyChanged(nameof(CarModel));
        }




        #region Properties
        


        private List<SectorModel> _SplitSectorInfo = new();
        public List<SectorModel> SplitSectorInfo
        {
            get => _SplitSectorInfo;
            set
            {
                _SplitSectorInfo = value;
                OnPropertyChanged(nameof(SplitSectorInfo));
            }
        }

        private int _NrOfSectors = 0;
        public int NrOfSectors
        {
            get => _NrOfSectors;
            set
            {
                _NrOfSectors = value;
                OnPropertyChanged(nameof(NrOfSectors));
            }
        }

      
        public Model3DGroup SectorOverlayModel { get; set; } = new Model3DGroup();



        private Point3D _carLabelTextPosition;
        public Point3D CarLabelTextPosition
        {
            get => _carLabelTextPosition;
            set
            {
                _carLabelTextPosition = value;
                OnPropertyChanged(nameof(CarLabelTextPosition));
            }
        }

        private double _LapPct = 0.0;
        public double LapPct
        {
            get => _LapPct;
            set
            {
                if (_LapPct != value)
                {
                    _LapPct = value;
                    UpdateCarPosition(_LapPct);
                    OnPropertyChanged(nameof(LapPct));
                }
            }
        }

        private MeshGeometry3D _trackMesh;
        public MeshGeometry3D TrackMesh
        {
            get => _trackMesh;
            set
            {
                _trackMesh = value;
                OnPropertyChanged(nameof(TrackMesh));
            }
        }


        private ObservableCollection<TrackPoint> _recordedPoints = new();
        public ObservableCollection<TrackPoint> RecordedPoints
        {
            get => _recordedPoints;
            set
            {
                _recordedPoints = value;
                OnPropertyChanged(nameof(RecordedPoints)); // 🔥 trigger UI refresh
            }
        }

        private PointCollection _polylinePoints;
        public PointCollection PolylinePoints
        {
            get => _polylinePoints;
            set
            {
                _polylinePoints = value;
                OnPropertyChanged(nameof(PolylinePoints));
            }
        }
        #endregion
    }
}
