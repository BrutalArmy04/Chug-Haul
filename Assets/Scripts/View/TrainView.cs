using System.Collections.Generic;
using UnityEngine;
using ChugHaul.Core;

namespace ChugHaul.View
{
    // Shows one Core train. It waits at its stop until Core dispatches it (TrainDeparted), then
    // walks the path segment by segment, turning at middle stops without stopping, and calls
    // CompleteTrip once at the destination. Core decides everything else.
    // Selectable: Id is the train's line id (one train per line); clicking it opens its orders.
    public class TrainView : SelectableView
    {
        [SerializeField] float speed = 8f;
        [SerializeField] float turnSpeed = 6f;

        [Header("Waiting for orders")]
        [SerializeField] GameObject waitingMarker; // child object, shown only while the train is AtStop
        [SerializeField] float markerBobHeight = 0.15f;
        [SerializeField] float markerBobSpeed = 3f;

        Economy economy;
        Train train;
        float stopPadding; // how far before a stop's centre the train halts

        // The stop the train last arrived from. Decides how it leaves: turning back the way it
        // came, or continuing through the stop's centre so it stays on the track.
        Node cameFrom;

        IReadOnlyList<Segment> path;
        readonly List<Vector3> waypoints = new List<Vector3>();
        readonly List<int> waypointSegment = new List<int>(); // index in path of the segment each waypoint is on
        int next = -1; // waypoint being approached; -1 = standing still

        // Where this trip started plus every waypoint reached so far. With the current position
        // it is exactly the track driven, corners included. Wagons follow it.
        readonly List<Vector3> trail = new List<Vector3>();
        public IReadOnlyList<Vector3> Trail => trail;

        Vector3 desiredDir;
        Vector3 markerRest;

        // The segment the train is on (null while it waits). The elevation task reads it in StepMovement.
        Segment CurrentSegment => next >= 0 ? path[waypointSegment[next]] : null;

        public void Init(Economy economy, Train train, float stopPadding)
        {
            this.economy = economy;
            this.train = train;
            this.stopPadding = stopPadding;
            Id = train.Line.Id;

            // Stand as if the train had just arrived at its stop from a neighbouring stop.
            Line line = train.Line;
            int index = line.IndexOf(train.CurrentStop);
            cameFrom = index + 1 < line.Stops.Count ? line.Stops[index + 1] : line.Stops[index - 1];

            transform.position = StopPoint(train.CurrentStop, cameFrom);
            FaceAfterArrival(train.CurrentStop, cameFrom);
            transform.rotation = Quaternion.LookRotation(desiredDir);

            if (waitingMarker != null)
                markerRest = waitingMarker.transform.localPosition;

            economy.TrainDeparted += OnTrainDeparted;
            RefreshMarker();
        }

        void OnDestroy()
        {
            if (economy != null) economy.TrainDeparted -= OnTrainDeparted;
        }

        void OnTrainDeparted(Train departed)
        {
            if (departed != train) return;
            Go(train.Line.PathBetween(train.CurrentStop, train.Destination));
        }

        // Walks the segments in order, without stopping in between.
        public void Go(IReadOnlyList<Segment> path)
        {
            if (path == null || path.Count == 0) return;

            this.path = path;
            waypoints.Clear();
            waypointSegment.Clear();

            // Not turning back: pass over the stop's centre first, so the train stays on the track.
            if (path[0].To != cameFrom)
                AddWaypoint(Pos(path[0].From), 0);

            // Middle stops: straight through their centre, no dwell.
            for (int i = 0; i < path.Count - 1; i++)
                AddWaypoint(Pos(path[i].To), i);

            Segment last = path[path.Count - 1];
            AddWaypoint(StopPoint(last.To, last.From), path.Count - 1);

            trail.Clear();
            trail.Add(transform.position);

            next = 0;
            RefreshMarker();
        }

        void Update()
        {
            if (next >= 0)
            {
                StepMovement(Time.deltaTime);
                if (next >= waypoints.Count)
                    Arrive();
            }

            if (desiredDir.sqrMagnitude > 0.0001f)
            {
                var want = Quaternion.LookRotation(desiredDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }

            if (waitingMarker != null && waitingMarker.activeSelf)
                waitingMarker.transform.localPosition =
                    markerRest + Vector3.up * (Mathf.Sin(Time.time * markerBobSpeed) * markerBobHeight);
        }

        // The only place that moves the train. M2 swaps in the momentum model here; the elevation
        // task multiplies the speed by the terrain factor of CurrentSegment.
        // Distance left over after reaching a waypoint carries on to the next one, so the speed
        // is the same at any frame rate.
        void StepMovement(float dt)
        {
            float distance = speed * dt;

            while (next < waypoints.Count)
            {
                Vector3 toTarget = waypoints[next] - transform.position;
                float remaining = toTarget.magnitude;

                if (remaining > 0.0001f)
                    desiredDir = toTarget / remaining;

                if (remaining > distance)
                {
                    transform.position += desiredDir * distance;
                    return;
                }

                transform.position = waypoints[next];
                trail.Add(waypoints[next]);
                distance -= remaining;
                next++;
            }
        }

        void Arrive()
        {
            Segment last = path[path.Count - 1];
            next = -1;
            path = null;
            cameFrom = last.From;

            economy.CompleteTrip(train.Line.Id); // exactly once per trip: Core pays and raises TrainArrived

            FaceAfterArrival(last.To, last.From);
            RefreshMarker();
        }

        // Facing after arriving at `stop` from `from`: the direction of travel. At either end of
        // the line the only way out is back, so the train turns around while it waits.
        void FaceAfterArrival(Node stop, Node from)
        {
            IReadOnlyList<Node> stops = train.Line.Stops;
            bool endOfLine = stop == stops[0] || stop == stops[stops.Count - 1];

            Vector3 dir = endOfLine ? Pos(from) - Pos(stop) : Pos(stop) - Pos(from);
            desiredDir = dir.normalized;
        }

        // Only while the train waits for orders. Core's state decides.
        void RefreshMarker()
        {
            if (waitingMarker != null)
                waitingMarker.SetActive(train.State == TrainState.AtStop);
        }

        void AddWaypoint(Vector3 point, int segmentIndex)
        {
            waypoints.Add(point);
            waypointSegment.Add(segmentIndex);
        }

        // Where the train halts at `stop` when coming from `from`: short of the centre, on that segment.
        Vector3 StopPoint(Node stop, Node from)
        {
            Vector3 back = Pos(from) - Pos(stop);
            return Pos(stop) + back.normalized * Mathf.Min(stopPadding, back.magnitude * 0.5f);
        }

        static Vector3 Pos(Node node) => new Vector3(node.X, 0f, node.Z);
    }
}
