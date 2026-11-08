using System;
using System.Collections;
using UnityEngine;

namespace ChugHaul.View
{
    public class TrainView : MonoBehaviour
    {
        [SerializeField] float speed = 8f;
        [SerializeField] float dwellTime = 1f;
        [SerializeField] float turnSpeed = 6f;

        public event Action<int> Arrived; // reports the line id; Core decides the reward

        int lineId;
        Vector3 pointA, pointB, target;
        Vector3 desiredDir;

        public void Init(int lineId, Vector3 a, Vector3 b, float endPadding)
        {
            this.lineId = lineId;
            Vector3 dir = (b - a).normalized;
            pointA = a + dir * endPadding;
            pointB = b - dir * endPadding;

            transform.position = pointA;
            target = pointB;
            desiredDir = (target - pointA).normalized;
            transform.rotation = Quaternion.LookRotation(desiredDir);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            while (true)
            {
                while ((transform.position - target).sqrMagnitude > 0.0001f)
                {
                    StepMovement();
                    yield return null;
                }
                transform.position = target;
                Arrived?.Invoke(lineId); // exactly once per arrival

                target = (target == pointB) ? pointA : pointB; // swap ends
                desiredDir = (target - transform.position).normalized; // turn during the dwell
                yield return new WaitForSeconds(dwellTime);
            }
        }

        // M2 replaces this with the Core momentum model
        void StepMovement()
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }

        void Update()
        {
            if (desiredDir.sqrMagnitude < 0.0001f) return;
            var want = Quaternion.LookRotation(desiredDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }
    }
}