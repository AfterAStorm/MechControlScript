using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {

        double calibrateStepTime = 0;
        int calibrationStep = -1;
        Vector3D startLoc;

        struct CalibrationResult
        {
            public double Distance;
            public float WalkCycleSpeed;
        }

        CalibrationResult? LastCalibration;
        CalibrationResult BestCalibration;

        void HandleCalibration(double delta)
        {
            if (cockpits.Count == 0)
                return; // needs a cockpit
            if (calibrationStep == -1)
            {
                calibrationStep = 0;
                LastCalibration = new CalibrationResult()
                {
                    WalkCycleSpeed = .9f
                };
                BestCalibration = new CalibrationResult()
                {
                    Distance = 0,
                    WalkCycleSpeed = 0
                };
            }
            var nextCalibration = new CalibrationResult()
            {
                WalkCycleSpeed = LastCalibration.Value.WalkCycleSpeed + .1f
            };
            if (nextCalibration.WalkCycleSpeed > 3)
            {
                calibrating = false;
                calibrationStep = -1;
                return;
            }
            var cockpit = cockpits.First();
            switch(calibrationStep)
            {
                case 0:
                    startLoc = cockpit.WorldMatrix.Translation;
                    movementOverride = Vector3.Forward;
                    calibrationStep += 1;
                    break;
                case 1:
                    calibrateStepTime += delta;
                    if (calibrateStepTime > 12)
                    {
                        calibrateStepTime = 0;
                        calibrationStep += 1;
                    }
                    break;
                case 2:
                    movementOverride = Vector3.Zero;
                    calibrateStepTime += delta;
                    if (calibrateStepTime > 6)
                    {
                        calibrateStepTime = 0;
                        calibrationStep += 1;
                    }
                    break;
                case 3:
                    double distanceMoved = Vector3D.Distance(startLoc, cockpit.WorldMatrix.Translation);
                    nextCalibration.Distance = distanceMoved;
                    if (BestCalibration.Distance < nextCalibration.Distance)
                        BestCalibration = nextCalibration;
                    LastCalibration = nextCalibration;
                    calibrationStep = 0;
                    break;
            }
            WalkCycleSpeed = nextCalibration.WalkCycleSpeed;
            
        }
    }
}
