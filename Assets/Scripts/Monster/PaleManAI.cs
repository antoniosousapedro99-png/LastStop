using UnityEngine;
using UnityEngine.AI;

// The Pale Man's behaviour. He is always in exactly one of these moods:
// - Wander: walks to random spots underground, now and then drifting toward where the player is.
// - Investigate: heard something; walks to the spot and looks around.
// - Chase: sees the player; runs at them (3.8 m/s, a little slower than sprinting).
// - Search: lost sight; goes to where he last saw them and looks around for a while.
// - Attack: close enough; lunges, and the player takes a hit if still in reach.
// - Retreat: after a hit, backs off for a few seconds so hits never come back to back.
// - Repelled: a lit flare is near; he backs away and keeps his distance until it dies.
// - Reveal: his first appearance. He stands at the end of the platform staring at the player; once
//   they've noticed him (a sting plays) he stares a moment longer, then...
// - WalkOff: ...walks away into the tunnel, ignoring the player, and only then starts hunting.
// A player hiding in a locker is invisible to him, unless he saw them get in: then he walks over,
// pulls them out and attacks.
// He walks on the underground navigation mesh only, so the street is safe.
[RequireComponent(typeof(NavMeshAgent), typeof(PaleManSenses))]
public class PaleManAI : MonoBehaviour
{
    public enum Mood { Wander, Investigate, Chase, Search, Attack, Retreat, Repelled, Reveal, WalkOff }

    public Animator animator;
    public float walkSpeed = 0.9f, chaseSpeed = 3.8f;
    public float attackRange = 1.6f, hitReach = 2.1f;
    public float loseAfter = 8f;               // seconds without seeing the player before giving up the chase
    public float flareKeepAway = 6f;           // metres he stays from a lit flare
    // How fast each animation's planted foot slides back along the ground at normal playback (measured on
    // the clips, in metres per second). The animation is sped up to match how fast he really moves, so his
    // feet don't skate, but only up to a limit, past which it looks like fast-forward.
    public float clipWalkSpeed = 0.28f, clipRunSpeed = 1.07f;
    public float maxWalkPlayback = 3.25f, maxRunPlayback = 3f;
    // Where in each animation's loop (0..1) a foot hits the ground, for the footstep sounds.
    public float[] walkFootfalls = { 0.37f, 0.87f }, runFootfalls = { 0.0f, 0.5f };
    public float revealStare = 2.5f;           // seconds he keeps staring after the player notices him

    [Header("Sounds")]
    public AudioSource voice;                  // breathing, shrieks
    public AudioSource feet;
    public AudioClip[] breaths, shrieks, steps, cracks;   // cracks: his joints snapping as he moves
    public AudioClip hitSound;
    public AudioSource sting;                  // the music sting when the player first sees him

    public Mood Current { get; private set; } = Mood.Wander;
    public bool SeesPlayer => seesPlayer;
    public bool SeenByPlayer { get; private set; }   // he's on the player's screen, not hidden behind anything

    // The one Pale Man in the level while he's awake (the torch flicker and the lockers ask about him).
    public static PaleManAI Active { get; private set; }

    NavMeshAgent agent;
    PaleManSenses senses;
    float moodTime, lastSeen, nextBreath, nextCrack, sightTimer, noticedAt = -1f;
    float smoothedSpeed, lastCycle;            // his speed, eased so the animation doesn't flick between walk and run
    Vector3 lastSeenAt, walkOffTo;
    bool hitDone, seesPlayer;
    string playing;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        senses = GetComponent<PaleManSenses>();
    }

    void OnEnable() { Active = this; SetMood(Mood.Wander); }
    void OnDisable() { if (Active == this) Active = null; }

    // Used by the arrival: he appears standing at 'standAt', and leaves toward 'leaveTo' once he's been seen.
    public void StageReveal(Vector3 standAt, Vector3 leaveTo)
    {
        agent.Warp(standAt);
        walkOffTo = leaveTo;
        noticedAt = -1f;
        if (senses.Player) FaceTowards(senses.Player.position, 1000f);
        SetMood(Mood.Reveal);
    }

    // Loading a save: back where he was, wandering.
    public void PutBack(Vector3 position, float yaw)
    {
        agent.Warp(position);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        SetMood(Mood.Wander);
    }

    void Update()
    {
        moodTime += Time.deltaTime;
        sightTimer -= Time.deltaTime;
        if (sightTimer <= 0f) { sightTimer = 0.2f; seesPlayer = senses.CanSeePlayer(); SeenByPlayer = senses.PlayerCanSeeMe(); }
        if (seesPlayer) { lastSeen = Time.time; lastSeenAt = senses.Player.position; }

        // A lit flare close by overrides everything else (except his staged first appearance).
        var flare = Flare.NearestLit(transform.position);
        bool staged = Current == Mood.Reveal || Current == Mood.WalkOff;
        if (flare && !staged && Current != Mood.Repelled && Vector3.Distance(flare.position, transform.position) < flareKeepAway)
            SetMood(Mood.Repelled);

        switch (Current)
        {
            case Mood.Wander: Wander(); break;
            case Mood.Investigate: Investigate(); break;
            case Mood.Chase: Chase(flare); break;
            case Mood.Search: Search(); break;
            case Mood.Attack: Attack(); break;
            case Mood.Retreat: Retreat(); break;
            case Mood.Repelled: Repelled(flare); break;
            case Mood.Reveal: Reveal(); break;
            case Mood.WalkOff: WalkOff(); break;
        }
        if (Current != Mood.Attack && Current != Mood.Reveal && agent.isStopped && !Arrived()) agent.isStopped = false;   // after a look-around
        Animate();
        Sounds();
    }

    void SetMood(Mood mood)
    {
        if (mood == Mood.Chase && Current != Mood.Chase && shrieks.Length > 0) Say(shrieks[Random.Range(0, shrieks.Length)], 1f);
        Current = mood;
        moodTime = 0f;
        hitDone = false;
        agent.isStopped = false;
        agent.speed = mood == Mood.Chase ? chaseSpeed : walkSpeed;
        if (mood == Mood.Wander) PickWanderSpot();
        if (mood == Mood.Search) agent.SetDestination(lastSeenAt);
        if (mood == Mood.Attack) { agent.isStopped = true; Play("Attack"); }
        if (mood == Mood.Reveal) { agent.isStopped = true; agent.SetDestination(transform.position); }
        if (mood == Mood.WalkOff) agent.SetDestination(walkOffTo);
        if (mood == Mood.Retreat || mood == Mood.Repelled) agent.speed = walkSpeed * 1.5f;
    }

    // Seeing the player starts a chase from any calm mood.
    bool StartChaseIfSeen()
    {
        if (!seesPlayer) return false;
        SetMood(Mood.Chase);
        return true;
    }

    bool InvestigateIfHeard()
    {
        if (!senses.HasHeard) return false;
        Vector3 spot = senses.HeardAt;
        senses.ForgetNoise();
        if (NavMesh.SamplePosition(spot, out var hit, 4f, NavMesh.AllAreas))
        {
            SetMood(Mood.Investigate);
            agent.SetDestination(hit.position);
            return true;
        }
        return false;
    }

    void Wander()
    {
        if (StartChaseIfSeen() || InvestigateIfHeard()) return;
        if (Arrived() || moodTime > 30f) SetMood(Mood.Wander);   // next spot
    }

    void PickWanderSpot()
    {
        // Sometimes drift toward the player's general area, so he never wanders off for good.
        Vector3 centre = transform.position;
        if (senses.Player && Random.value < 0.4f) centre = senses.Player.position;
        for (int i = 0; i < 10; i++)
        {
            Vector2 r = Random.insideUnitCircle * 20f;
            if (NavMesh.SamplePosition(centre + new Vector3(r.x, 0f, r.y), out var hit, 4f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    void Investigate()
    {
        if (StartChaseIfSeen()) return;
        if (senses.HasHeard) { InvestigateIfHeard(); return; }
        if (Arrived()) { agent.isStopped = true; LookAround(); }
        if (moodTime > 14f) SetMood(Mood.Wander);
    }

    void Chase(Transform flare)
    {
        if (!senses.Player) { SetMood(Mood.Wander); return; }
        var locker = HidingLocker.Occupied;
        if (locker)
        {
            // Hidden. If he watched them get in he goes straight for that locker; otherwise he's lost them.
            if (!HidingLocker.SeenGettingIn) { SetMood(Mood.Search); return; }
            agent.SetDestination(locker.StandPoint);
            if (Vector3.Distance(transform.position, locker.StandPoint) < 1.3f)
            {
                locker.PullOut();
                SetMood(Mood.Attack);
            }
            return;
        }
        Vector3 target = senses.Player.position;
        // The player holds a lit flare: circle at a distance instead of closing in.
        if (flare && Vector3.Distance(flare.position, target) < flareKeepAway)
            target += (transform.position - target).normalized * (flareKeepAway + 0.5f);
        agent.SetDestination(target);
        float distance = Vector3.Distance(transform.position, senses.Player.position);
        if (seesPlayer && distance < attackRange && !(flare && Vector3.Distance(flare.position, transform.position) < flareKeepAway + 1f))
        {
            SetMood(Mood.Attack);
            return;
        }
        if (Time.time - lastSeen > loseAfter) SetMood(Mood.Search);
    }

    void Search()
    {
        if (StartChaseIfSeen()) return;
        if (Arrived()) { agent.isStopped = true; LookAround(); }
        if (moodTime > 10f) SetMood(Mood.Wander);
    }

    void Attack()
    {
        if (senses.Player) FaceTowards(senses.Player.position, 8f);
        // The lunge lands about 0.45 s in; it only hurts if the player is still within reach.
        if (!hitDone && moodTime > 0.45f)
        {
            hitDone = true;
            var health = senses.Player ? senses.Player.GetComponent<PlayerHealth>() : null;
            if (health && Vector3.Distance(transform.position, senses.Player.position) < hitReach)
            {
                if (hitSound) feet.PlayOneShot(hitSound, 1f);
                health.Hit(transform.position);
            }
        }
        if (moodTime > 1.2f) SetMood(Mood.Retreat);
    }

    void Retreat()
    {
        if (moodTime < 0.1f || Arrived())
        {
            // Back off away from the player.
            Vector3 away = transform.position + (transform.position - (senses.Player ? senses.Player.position : transform.position)).normalized * 8f;
            if (NavMesh.SamplePosition(away, out var hit, 5f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        }
        if (moodTime > 6f) SetMood(Mood.Search);
    }

    void Repelled(Transform flare)
    {
        if (!flare || Vector3.Distance(flare.position, transform.position) > flareKeepAway + 1.5f)
        {
            if (moodTime > 1f) SetMood(seesPlayer ? Mood.Chase : Mood.Search);
            return;
        }
        Vector3 away = transform.position + (transform.position - flare.position).normalized * 4f;
        if (NavMesh.SamplePosition(away, out var hit, 3f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        FaceTowards(flare.position, 4f);   // backs away, never takes his eyes off it
    }

    // Stands still and stares. When the player notices him a sting plays; a moment later he turns away.
    // He also leaves if the player walks right up to him, or never comes to look.
    void Reveal()
    {
        if (senses.Player) FaceTowards(senses.Player.position, 3f);
        if (noticedAt < 0f && SeenByPlayer)
        {
            noticedAt = Time.time;
            if (sting) { sting.volume = GameSettings.MusicLevel; sting.Play(); }
        }
        bool close = senses.Player && Vector3.Distance(transform.position, senses.Player.position) < 5f;
        if ((noticedAt >= 0f && Time.time - noticedAt > revealStare) || close || moodTime > 30f)
        {
            if (cracks.Length > 0) Say(cracks[Random.Range(0, cracks.Length)], 0.8f);   // his neck snaps as he turns
            SetMood(Mood.WalkOff);
        }
    }

    // Walks into the tunnel without looking back; only once he's there does the hunt begin.
    void WalkOff()
    {
        if (Arrived() || moodTime > 40f)
        {
            senses.ForgetNoise();
            SetMood(Mood.Wander);
        }
    }

    bool Arrived() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;

    void LookAround() => transform.Rotate(0f, Mathf.Sin(Time.time * 0.8f) * 60f * Time.deltaTime, 0f);

    void FaceTowards(Vector3 point, float speed)
    {
        Vector3 to = point - transform.position; to.y = 0f;
        if (to.sqrMagnitude < 0.01f) return;
        agent.updateRotation = false;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), speed * Time.deltaTime);
    }

    // Picks the animation from how fast he's actually moving, and speeds it up or slows it down so his
    // feet match the ground.
    void Animate()
    {
        if (Current != Mood.Repelled && Current != Mood.Attack && Current != Mood.Reveal) agent.updateRotation = true;
        if (Current == Mood.Attack) return;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, agent.velocity.magnitude, 1f - Mathf.Exp(-8f * Time.deltaTime));
        float speed = smoothedSpeed;
        // Two thresholds, so hovering around one speed doesn't keep swapping walk and run.
        bool run = playing == "Run" ? speed > 2.0f : speed > 2.6f;
        if (run) { Play("Run"); animator.speed = Mathf.Clamp(speed / clipRunSpeed, 1f, maxRunPlayback); }
        else if (speed > 0.15f) { Play("Walk"); animator.speed = Mathf.Clamp(speed / clipWalkSpeed, 0.8f, maxWalkPlayback); }
        else { Play("Idle"); animator.speed = 1f; }
    }

    public void Play(string state)
    {
        if (playing == state) return;
        playing = state;
        if (state == "Attack" || state == "Scream") animator.speed = 1f;
        animator.CrossFadeInFixedTime(state, 0.25f);
        lastCycle = 0f;
    }

    void Sounds()
    {
        // A footstep each time a foot lands in the walk or run animation, so sound and picture match.
        if ((playing == "Walk" || playing == "Run") && steps.Length > 0)
        {
            float cycle = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
            foreach (float landing in playing == "Run" ? runFootfalls : walkFootfalls)
                if (Crossed(lastCycle, cycle, landing)) { feet.pitch = Random.Range(0.92f, 1.05f); feet.PlayOneShot(steps[Random.Range(0, steps.Length)], 0.9f); }
            lastCycle = cycle;
            // Now and then a joint snaps.
            if (Time.time > nextCrack && cracks.Length > 0)
            {
                if (nextCrack > 0f) Say(cracks[Random.Range(0, cracks.Length)], 0.5f);
                nextCrack = Time.time + Random.Range(5f, 12f);
            }
        }
        // Wet, low breathing every few seconds.
        if (Time.time > nextBreath && breaths.Length > 0)
        {
            nextBreath = Time.time + Random.Range(3f, 6f);
            Say(breaths[Random.Range(0, breaths.Length)], 0.7f);
        }
    }

    // Did the loop position move past 'mark' since last frame (allowing for the wrap from 1 back to 0)?
    static bool Crossed(float from, float to, float mark)
    {
        if (to >= from) return mark > from && mark <= to;
        return mark > from || mark <= to;
    }

    void Say(AudioClip clip, float volume)
    {
        voice.pitch = Random.Range(0.92f, 1.05f);   // the clips are already pitched down; a little variety
        voice.PlayOneShot(clip, volume);
    }
}
