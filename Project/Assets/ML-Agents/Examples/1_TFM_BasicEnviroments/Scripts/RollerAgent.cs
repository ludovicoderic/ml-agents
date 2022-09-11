using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// import ML-Agents package 
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


// Delete Update() since we are not using it, but keep Start()
public class RollerAgent : Agent{
    // reference to the Rigidbody component to reset the Agent's velocity and later to apply force to it 
    Rigidbody rBody;

    public Material winMaterial;
    public Material loseMaterial;
    public Material normalMaterial;
    public MeshRenderer floorMeshRenderer;

    void Start(){
        rBody = GetComponent<Rigidbody>();
    }

    // public field of type Transform to the RollerAgent class for reference to move the target
    public Transform Target; 

    // set-up the environment for a new episode
    public override void OnEpisodeBegin()
    {
        // If the Agent fell of the platform, zero its momentum
        if (this.transform.localPosition.y < 0)
        {
            // reset the Agent's velocity and put back onto the floor
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        // Move the target to a new spot (moved to a new random location)
        Target.localPosition = new Vector3(Random.value * 8 - 4,
                                           0.5f,
                                           Random.value * 8 - 4);
    }

    // Observing the Environment (what information to collect), 8 values
    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions 
        sensor.AddObservation(Target.localPosition);            // Target (x,y,z)
        sensor.AddObservation(this.transform.localPosition);    // Agent  (x,y,z)

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);                // Agent Vel (x)
        sensor.AddObservation(rBody.velocity.z);                // Agent Vel (z)
    }

    // Taking Actions and Assigning Rewards
    // to move towards the target the agent needs 2 actions: determines the force applied along the x-axis and the z-axis
    public float forceMultiplier = 10; // public class variable (can set the value from the Inspector window)
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0]; // force applied along the x-axis
        controlSignal.z = actionBuffers.ContinuousActions[1]; // force applied along the z-axis
        // RollerAgent applies the values from the action[] array to its Rigidbody component rBody, using Rigidbody.AddForce()
        rBody.AddForce(controlSignal * forceMultiplier);

        // Rewards
        // calculates the distance to detect when it reaches the target
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        // Reached target
        // the Agent is given a reward of 1.0 for reaching the Target cube
        if (distanceToTarget < 1.42f)
        {
            SetReward(1.0f);
            EndEpisode();
            StartCoroutine(
                GoalScoredSwapGroundMaterial(winMaterial, 2));

        }

        // Fell off platform
        // if the Agent falls off the platform, end the episode so that it can reset itself
        else if (this.transform.localPosition.y < 0)
        {
            EndEpisode();
            StartCoroutine(
               GoalScoredSwapGroundMaterial(loseMaterial, 2));
        }
    }

    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        floorMeshRenderer.material = mat;
        yield return new WaitForSeconds(time); //wait for 2 sec
        floorMeshRenderer.material = normalMaterial;
    }

    // to first test your environment by controlling the Agent using the keyboard, extending the Heuristic() method
    // the heuristic will generate an action corresponding to the values of the "Horizontal" and "Vertical"
    // input axis (which correspond to the keyboard arrow keys)
    public override void Heuristic(in ActionBuffers actionsOut){
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

}
