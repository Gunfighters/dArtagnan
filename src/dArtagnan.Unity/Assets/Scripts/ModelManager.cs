using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;

public class ModelManager : MonoBehaviour
{
    public Character4D actualModel;
    public Character4D modelSilhouette;
    public CharacterState initialState;
    public Vector2 direction;
    public float spriteAlpha;
    public AudioSource fireSound;
    public FirearmCollection firearmCollection;
    private readonly List<ParticleSystem> _instances = new();
    private AudioClip ShotSoundClip;
    private LineRenderer trajectory;
    public float trajectoryDuration;
    public float trajectoryOpacity;
    private Color trajectoryColorOriginal;
    private Transform trajectoryTarget;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        InitializeTrajectory();
        SetTransparent();
        SetDirection(direction == Vector2.zero ? Vector2.down : direction);
        SetState(initialState);
        InitializeFirearmMuzzle();
    }

    private void Update()
    {
        if (trajectoryTarget)
        {
            trajectory.SetPosition(0, trajectoryTarget.position);
        }
        trajectory.SetPosition(1, transform.position);
    }

    void InitializeTrajectory()
    {
        trajectory = GetComponent<LineRenderer>();
        trajectory.enabled = false;
        trajectory.SetPosition(0, Vector2.zero);
        // trajectoryColorOriginal = trajectory.material.color;
    }
    
    void SetTransparent()
    {
        var sprites = modelSilhouette.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var spriteRenderer in sprites)
        {
            var modified = spriteRenderer.color;
            modified.a = spriteAlpha;
            spriteRenderer.color = modified;
        }
    }

    void SetState(CharacterState state)
    {
        actualModel.SetState(state);
        modelSilhouette.SetState(state);
    }

    public void SetDirection(Vector2 newDir)
    {
        direction = SnapToCardinalDirection(newDir);
        actualModel.SetDirection(direction);
        modelSilhouette.SetDirection(direction);
    }

    public void Walk()
    {
        SetState(CharacterState.Walk);
    }

    public void Run()
    {
        SetState(CharacterState.Run);
    }

    public void Idle()
    {
        SetState(CharacterState.Idle);
    }

    public void Fire()
    {
        actualModel.AnimationManager.Fire();
        modelSilhouette.AnimationManager.Fire();
        CreateFirearmMuzzleAndPlayShotSound();
    }
    
    public void Die()
    {
        actualModel.SetState(CharacterState.Death);
        modelSilhouette.SetState(CharacterState.Death);
    }

    public void ShowTrajectory(Transform target, bool transparent = false)
    {
        trajectoryTarget = target;
        trajectory.enabled = true;
    }

    public void HideTrajectory()
    {
        trajectory.enabled = false;
    }

    public void ScheduleHideTrajectory()
    {
        StartCoroutine(HideTrajectoryAfterDuration());
    }

    IEnumerator HideTrajectoryAfterDuration()
    {
        yield return new WaitForSeconds(trajectoryDuration);
        HideTrajectory();
    }

    private FirearmParams GetFirearmParams()
    {
        if (actualModel.Parts[0].PrimaryWeapon is null)
        {
            throw new Exception($"PrimaryWeapon not set");
        }
        var firearm =
            actualModel.SpriteCollection.Firearm1H.SingleOrDefault(i =>
                i.Sprites.Contains(actualModel.Parts[0].PrimaryWeapon))
            ?? actualModel.SpriteCollection.Firearm2H.SingleOrDefault(i =>
                i.Sprites.Contains(actualModel.Parts[0].PrimaryWeapon));
        if (firearm is null)
        {
            throw new Exception($"Firearm sprite not found");
        }

        var fallback = firearm.Collection;
        var foundParams = firearmCollection.FirearmParams.SingleOrDefault(i => i.Name == firearm.Name)
                           ?? firearmCollection.FirearmParams.SingleOrDefault(i => i.Name == fallback)
                           ?? firearmCollection.FirearmParams.SingleOrDefault(i => i.Name == "Basic");
        if (foundParams is null)
        {
            throw new Exception($"Firearm params not found for {firearm.Name}.");
        }
        return foundParams;
    }

    private void CreateFirearmMuzzleAndPlayShotSound()
    {
        foreach (var muzzle in _instances.Where(i => i.gameObject.activeInHierarchy)) muzzle.Play(true);
        fireSound.PlayOneShot(ShotSoundClip);
    }

    private void InitializeFirearmMuzzle()
    {
        var firearmParams = GetFirearmParams();
        if (_instances.Count > 0)
        {
            _instances.ForEach(i => Destroy(i.gameObject));
            _instances.Clear();
        }

        for (var i = 0; i < 4; i++)
        {
            var anchor = actualModel.Parts[i].AnchorFireMuzzle;
            var muzzle = Instantiate(firearmParams.FireMuzzlePrefab, anchor);

            _instances.Add(muzzle);
        }
        ShotSoundClip = firearmParams.ShotSound;
    }
    static Vector3 SnapToCardinalDirection(Vector3 dir)
    {
        if (dir == Vector3.zero) return Vector3.zero;

        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        return angle switch
        {
            >= 45f and < 135f => Vector3.up,
            >= 135f and < 225f => Vector3.left,
            >= 225f and < 315f => Vector3.down,
            _ => Vector3.right
        };
    }
}