// ================================================================================================================================
// File:        PerformanceGraph.cs
// Description: Rendered in the server window during runtime to monitor performance metrics of the application
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using Server.Logic;
using Server.Enums;
using ServerUtilities;
using BepuUtilities;
using ContentRenderer;
using ContentRenderer.UI;

namespace Server.Interface
{
    public class PerformanceGraph
    {
        public Graph DisplayGraph = null;  //The actual graph object to be rendered in the server windows UI
        public TimingDisplayMode GraphDisplayMode; //Current display mode used by the performance graph

        //Default Constructor which automatically sets up the graph in Regular timing mode
        public PerformanceGraph(Font UIFont, SimulationTimeSamples TimeSamples)
        {
            DisplayGraph = new Graph(new GraphDescription
            {
                BodyLineColor = new Vector3(1, 1, 1),
                AxisLabelHeight = 16,
                AxisLineRadius = 0.5f,
                HorizontalAxisLabel = "Frames",
                VerticalAxisLabel = "Time (ms)",
                VerticalIntervalValueScale = 1e3f,
                VerticalIntervalLabelRounding = 2,
                BackgroundLineRadius = 0.125f,
                IntervalTextHeight = 12,
                IntervalTickRadius = 0.25f,
                IntervalTickLength = 6f,
                TargetHorizontalTickCount = 5,
                HorizontalTickTextPadding = 0,
                VerticalTickTextPadding = 3,

                LegendMinimum = new Vector2(20, 200),
                LegendNameHeight = 12,
                LegendLineLength = 7,

                TextColor = new Vector3(1, 1, 1),
                Font = UIFont,

                LineSpacingMultiplier = 1f,

                ForceVerticalAxisMinimumToZero = true
            });

            DisplayGraph.AddSeries("Total", new Vector3(1, 1, 1), 0.75f, TimeSamples.Simulation);
            DisplayGraph.AddSeries("Pose Integrator", new Vector3(0, 0, 1), 0.25f, TimeSamples.PoseIntegrator);
            DisplayGraph.AddSeries("Sleeper", new Vector3(0.5f, 0, 1), 0.25f, TimeSamples.Sleeper);
            DisplayGraph.AddSeries("Broad Update", new Vector3(1, 1, 0), 0.25f, TimeSamples.BroadPhaseUpdate);
            DisplayGraph.AddSeries("Collision Test", new Vector3(0, 1, 0), 0.25f, TimeSamples.CollisionTesting);
            DisplayGraph.AddSeries("Narrow Flush", new Vector3(1, 0, 1), 0.25f, TimeSamples.NarrowPhaseFlush);
            DisplayGraph.AddSeries("Solver", new Vector3(1, 0, 0), 0.5f, TimeSamples.Solver);
            DisplayGraph.AddSeries("Body Opt", new Vector3(1, 0.5f, 0), 0.125f, TimeSamples.BodyOptimizer);
            DisplayGraph.AddSeries("Constraint Opt", new Vector3(0, 0.5f, 1), 0.125f, TimeSamples.ConstraintOptimizer);
            DisplayGraph.AddSeries("Batch Compress", new Vector3(0, 0.5f, 0), 0.125f, TimeSamples.BatchCompressor);
        }

        //Changes the graph on to the next display mode, or changes back to the first display mode type if already in the last type
        public void ChangeToNextDisplayMode(Window ApplicationWindow)
        {
            //Cast the current display mode enum to integer value
            int DisplayModeValue = (int)GraphDisplayMode;
            //Change to the next value, wrapping back to the first value if we go past the end
            DisplayModeValue = (DisplayModeValue + 1) > 2 ? 0 : (DisplayModeValue + 1);
            //Cast this back to the enum type and apply that to the performance graph
            GraphDisplayMode = (TimingDisplayMode)DisplayModeValue;
            //Finally change the graph to the new timing mode
            UpdateGraphTimingMode(GraphDisplayMode, ApplicationWindow);
        }

        //Changes the display mode for the performance monitor
        public void UpdateGraphTimingMode(TimingDisplayMode NewDisplayMode, Window ApplicationWindow)
        {
            //Store the new timing mode
            GraphDisplayMode = NewDisplayMode;
            //Fetch the current graph settings
            ref var Description = ref DisplayGraph.Description;
            //Get the current application window resolution
            Int2 Resolution = ApplicationWindow.Resolution;

            //Update the graphs display settings to match the new display mode
            switch(GraphDisplayMode)
            {
                case TimingDisplayMode.Big:
                    {
                        const float Inset = 150;
                        Description.BodyMinimum = new Vector2(Inset);
                        Description.BodySpan = new Vector2(Resolution.X, Resolution.Y) - Description.BodyMinimum - new Vector2(Inset);
                        Description.LegendMinimum = Description.BodyMinimum - new Vector2(110, 0);
                        Description.TargetVerticalTickCount = 5;
                    }
                    break;
                case TimingDisplayMode.Regular:
                    {
                        const float Inset = 50;
                        var TargetSpan = new Vector2(400, 150);
                        Description.BodyMinimum = new Vector2(Resolution.X - TargetSpan.X - Inset, Inset);
                        Description.BodySpan = TargetSpan;
                        Description.LegendMinimum = Description.BodyMinimum - new Vector2(130, 0);
                        Description.TargetVerticalTickCount = 3;
                    }
                    break;
            }
            //In the minimized state the graph is simply hidden from view
        }

        //Renders the performance graph to the server application window
        public void RenderGraph(TextBuilder UIText, Renderer Renderer)
        {
            //Only render the graph when the display mode is set to something other than the minimized state
            if (GraphDisplayMode != TimingDisplayMode.Minimized)
                DisplayGraph.Draw(UIText, Renderer.UILineBatcher, Renderer.TextBatcher);
        }
    }
}