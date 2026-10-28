using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Frankie's behaviour. He is always in exactly one of these moods:
// - Wander: walks to random spots underground, now and then drifting toward where the player is.
// - Investigate: heard something; walks to the spot and looks around.
// - Chase: sees the player; runs at them (3.5 m/s, slower than sprinting).
// - Search: lost sight; goes to where he last saw them and looks around for a while.
// - Attack: close enough; lunges forward about 1.5 m, and the player takes a hit if still in reach.
//   A miss sends him straight back into the chase.
// - Retreat: after a hit that landed, a short breather: he steps back a little, staring, then comes at the player
//   again (he keeps attacking until they die, escape out of sight or drive him off with a flare).
// - Repelled: a lit flare is near; he backs away and keeps his distance until it dies.
// - Reveal: his first appearance. He stands at the end of the platform staring at the player; once
//   they've noticed him (a sting plays) he stares a moment longer, then...
// - WalkOff: ...walks away into the tunnel, ignoring the player, and only then starts hunting.
// A player hiding in a locker is invisible to him, unless he knows they got in (HidingLocker decides):
// then he walks over and breaks in (BreakIn: slams the door three times, tears it open, pulls them out)
// and attacks.
// He walks on the underground navigation mesh only, so the street is safe. Where the mesh has a
// jump link (all along the platform edges) he leaps down to the tracks or up onto the platform.
[RequireComponent(typeof(NavMeshAgent), typeof(FrankieSenses))]
public class FrankieAI : MonoBehaviour
{
    public enum Mood { Wander, Investigate, Chase, Search, Attack, Retreat, Repelled, Reveal, WalkOff, BreakIn }

    public Animator animator;
    public float walkSpeed = 0.9f, chaseSpeed = 3.5f;
    public float attackRange = 2f, hitReach = 2.5f;   // lunges within 2 m; the hit lands if still within 2.5 m
    public float loseAfter = 8f;               // seconds without seeing the player before giving up the chase
    public float flareKeepAway = 6f;           // metres he stays from a lit flare
    // How fast each animation's planted foot slides back along the ground at normal playback (measured on
    // Frankie's clips at 2.1 m tall, in metres per second). The animation is sped up or slowed down to match
    // how fast he really moves, so his feet don't skate, within limits so it never looks like fast-forward.
    public float clipWalkSpeed = 0.8f, clipRunSpeed = 5.5f;
    public float maxWalkPlayback = 2f, maxRunPlayback = 1.5f, minRunPlayback = 0.6f;
    // Where in each animation's loop (0..1) a foot hits the ground, for the footstep sounds.
    public float[] walkFootfalls = { 0.43f, 0.83f }, runFootfalls = { 0.44f, 0.94f };
    // His attack: when the claws land and how long it lasts (seconds). The animation's claws land 0.8 s
    // into the clip ('clipHitAt'), so it is sped up to match 'hitAt'.
    public float hitAt = 0.5f, attackLength = 1f, clipHitAt = 0.8f;
    public float lungeDistance = 1.5f;         // how far he leaps forward before the claws land
    public float hitArc = 50f;                 // the claws only catch a player within this angle of straight ahead
    public float leapHeight = 0.6f, leapSpeed = 4f;   // his jumps between platform and tracks
    public float revealStare = 2.5f;           // seconds he keeps staring after the player notices him
    public float breather = 3f;                // seconds between one hit and coming at the player again
    public int lockerBangs = 3;                // slams on a locker door before he tears it open
    public float bangEvery = 0.9f;             // seconds between slams

    [Header("Sounds")]
    public AudioSource voice;                  // breathing, shrieks
    public AudioSource feet;
    public AudioClip[] breaths, shrieks, steps, cracks;   // cracks: his joints snapping as he moves
    public AudioClip hitSound;
    public AudioSource sting;                  // the music sting when the player first sees him

    public Mood Current { get; private set; } = Mood.Wander;
    public bool SeesPlayer => seesPlayer;
    public float LastSeenTime => lastSeen;     // Time.time he last saw the player
    public bool SeenByPlayer { get; private set; }   // he's on the player's screen, not hidden behind anything

    // The one Frankie in the level while he's awake (the torch flicker and the lockers ask about him).
    public static FrankieAI Active { get; private set; }

    NavMeshAgent agent;
    FrankieSenses senses;
    float moodTime, lastSeen, nextBreath, nextCrack, sightTimer, noticedAt = -1f;
    float huntUntil;                           // until then he never gives up a chase (the train finale)
    float smoothedSpeed, lastCycle;            // his speed, eased so the animation doesn't flick between walk and run
    Vector3 lastSeenAt, walkOffTo;
    HidingLocker breakingInto;
    int bangsDone;
    bool hitDone, landed, leaping, seesPlayer, stingPlayed;
    float searchArrivedAt = -1f;               // when a search reached its spot (moodTime), or -1
    string playing;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        senses = GetComponent<FrankieSenses>();
    }

    void OnEnable() { Active = this; leaping = false; SetMood(Mood.Wander); }
    void OnDisable() { if (Active == this) Active = null; }

    // Used by the arrival: he appears standing at 'standAt', and leaves toward 'leaveTo' once he's been seen.
    public void StageReveal(Vector3 standAt, Vector3 leaveTo)
    {
        StopLeap();
        agent.Warp(standAt);
        walkOffTo = leaveTo;
        noticedAt = -1f;
        if (senses.Player) FaceTowards(senses.Player.position, 1000f);
        SetMood(Mood.Reveal);
    }

    // In the train finale he lunges in through the cab door from the crate outside, so he reaches further.
    float AttackRange => Time.time < huntUntil ? attackRange + 0.8f : attackRange;
    float HitReach => Time.time < huntUntil ? hitReach + 0.6f : hitReach;

    // The train finale: he knows where the player is and runs straight for them for 'seconds'.
    public void Hunt(float seconds)
    {
        huntUntil = Time.time + seconds;
        lastSeen = Time.time;
        if (senses.Player) lastSeenAt = senses.Player.position;
        SetMood(Mood.Chase);
    }

    // Told roughly where the player is (Station 1: they've gone down to the service level). He walks
    // over and searches, without knowing exactly where they are.
    public void SearchFor(Vector3 near)
    {
        lastSeenAt = NavMesh.SamplePosition(near, out var hit, 4f, NavMesh.AllAreas) ? hit.position : near;
        SetMood(Mood.Search);
    }

    // Loading a save: back where he was, wandering.
    public void PutBack(Vector3 position, float yaw)
    {
        StopLeap();
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
        // The first time the player gets a clear look at him (outside his staged reveal), the sting plays.
        if (SeenByPlayer && !stingPlayed && Current != Mood.Reveal)
        {
            stingPlayed = true;
            if (sting) { sting.volume = GameSettings.MusicLevel; sting.Play(); }
        }

        // Reached a jump link (a platform edge): leap across it; nothing else happens mid-air.
        if (agent.isOnOffMeshLink)
        {
            if (!leaping) StartCoroutine(Leap());
            Sounds();
            return;
        }

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
            case Mood.BreakIn: BreakIn(); break;
        }
        if (Current != Mood.Attack && Current != Mood.Reveal && Current != Mood.BreakIn && agent.isStopped && !Arrived()) agent.isStopped = false;   // after a look-around
        Animate();
        Sounds();
    }

    void SetMood(Mood mood)
    {
        if (mood == Mood.Chase && Current != Mood.Chase && shrieks.Length > 0) Say(shrieks[Random.Range(0, shrieks.Length)], 1f);
        Current = mood;
        moodTime = 0f;
        hitDone = false;
        landed = false;
        searchArrivedAt = -1f;
        agent.isStopped = false;
        agent.speed = mood == Mood.Chase ? chaseSpeed : walkSpeed;
        if (mood == Mood.Wander) PickWanderSpot();
        if (mood == Mood.Search) agent.SetDestination(lastSeenAt);
        if (mood == Mood.Attack) { agent.isStopped = true; Play("Attack"); animator.speed = clipHitAt / hitAt; }
        if (mood == Mood.Reveal) { agent.isStopped = true; agent.SetDestination(transform.position); }
        if (mood == Mood.BreakIn) { agent.isStopped = true; bangsDone = 0; }
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
                breakingInto = locker;
                SetMood(Mood.BreakIn);
            }
            return;
        }
        Vector3 target = senses.Player.position;
        // The player holds a lit flare: circle at a distance instead of closing in.
        if (flare && Vector3.Distance(flare.position, target) < flareKeepAway)
            target += (transform.position - target).normalized * (flareKeepAway + 0.5f);
        agent.SetDestination(target);
        float distance = Vector3.Distance(transform.position, senses.Player.position);
        if (seesPlayer && distance < AttackRange && !(flare && Vector3.Distance(flare.position, transform.position) < flareKeepAway + 1f))
        {
            SetMood(Mood.Attack);
            return;
        }
        if (Time.time - lastSeen > loseAfter && Time.time > huntUntil) SetMood(Mood.Search);
    }

    // Walks to where he last saw (or was told about) the player, then looks around there for 10 seconds
    // before giving up (or gives up after a minute if he never gets there).
    void Search()
    {
        if (StartChaseIfSeen()) return;
        if (Arrived())
        {
            if (searchArrivedAt < 0f) searchArrivedAt = moodTime;
            agent.isStopped = true;
            LookAround();
        }
        if ((searchArrivedAt >= 0f && moodTime - searchArrivedAt > 10f) || moodTime > 60f) SetMood(Mood.Wander);
    }

    void Attack()
    {
        // During the lunge he's committed and turns only slowly; once the claws have landed he turns freely.
        if (senses.Player) FaceTowards(senses.Player.position, hitDone ? 8f : 2.5f);
        // The lunge: until the claws land he surges forward the way he's facing, so running away doesn't
        // escape it, but a well-timed sidestep can leave him swiping at air. He stops short of the player.
        if (!hitDone && senses.Player && Vector3.Distance(transform.position, senses.Player.position) > 1f)
            agent.Move(transform.forward * (lungeDistance / hitAt) * Time.deltaTime);
        // The claws land 'hitAt' seconds in; it only hurts if the player is still within reach, in front of him.
        if (!hitDone && moodTime > hitAt)
        {
            hitDone = true;
            var health = senses.Player ? senses.Player.GetComponent<PlayerHealth>() : null;
            Vector3 toPlayer = senses.Player ? senses.Player.position - transform.position : Vector3.zero;
            toPlayer.y = 0f;
            if (health && toPlayer.magnitude < HitReach && Vector3.Angle(transform.forward, toPlayer) < hitArc)
            {
                landed = true;
                if (hitSound) feet.PlayOneShot(hitSound, 1f);
                health.Hit(transform.position);
            }
        }
        // Missed: no breather, straight back after them (unless they've slipped into a locker unseen).
        if (hitDone && !landed && moodTime > hitAt + 0.25f)
        {
            lastSeen = Time.time;
            if (senses.Player) lastSeenAt = senses.Player.position;
            SetMood(HidingLocker.Occupied && !HidingLocker.SeenGettingIn ? Mood.Search : Mood.Chase);
            return;
        }
        if (moodTime > attackLength) SetMood(Mood.Retreat);
    }

    // At a locker he knows the player is in: a claw-swing at the door every 'bangEvery' seconds (each one
    // lands with a bang and a shudder), then he tears it open, drags them out and attacks.
    void BreakIn()
    {
        if (!breakingInto || HidingLocker.Occupied != breakingInto) { SetMood(Mood.Chase); return; }   // they got out
        FaceTowards(breakingInto.transform.position, 8f);
        float swingAt = bangsDone * bangEvery;            // when the current swing starts
        if (bangsDone < lockerBangs)
        {
            if (moodTime >= swingAt && playing != "Bang" + bangsDone)
            {
                playing = "Bang" + bangsDone;             // restart the claw swing for each slam
                animator.CrossFadeInFixedTime("Attack", 0.1f, 0, 0f);
                animator.speed = clipHitAt / 0.55f;        // the claws hit the door 0.55 s into the swing
            }
            if (moodTime >= swingAt + 0.55f) { breakingInto.Bang(); bangsDone++; }
            return;
        }
        if (moodTime >= lockerBangs * bangEvery + 0.2f)
        {
            breakingInto.PullOut();
            breakingInto = null;
            SetMood(Mood.Attack);
        }
    }

    void Retreat()
    {
        if (!senses.Player) { SetMood(Mood.Search); return; }
        if (moodTime < 0.1f)
        {
            // A step or two back, away from the player.
            Vector3 away = transform.position + (transform.position - senses.Player.position).normalized * 2f;
            if (NavMesh.SamplePosition(away, out var hit, 2f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        }
        FaceTowards(senses.Player.position, 6f);   // never takes his eyes off them
        if (moodTime > breather)
        {
            // He knows exactly where they are: straight back into the chase.
            lastSeen = Time.time;
            lastSeenAt = senses.Player.position;
            SetMood(HidingLocker.Occupied && !HidingLocker.SeenGettingIn ? Mood.Search : Mood.Chase);
        }
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
            stingPlayed = true;
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

    // Crossing a link. Along a staircase or through a doorway (a LinkPath) he walks its points; anywhere
    // else (a platform edge) it's one jump: an arc from where he stands to the link's far end.
    IEnumerator Leap()
    {
        leaping = true;
        var link = agent.currentOffMeshLinkData;
        var route = LinkPath.Find(link.startPos, link.endPos);
        if (route) yield return WalkRoute(route, link.startPos);
        else yield return Jump(link.endPos);
        agent.CompleteOffMeshLink();
        leaping = false;
    }

    IEnumerator WalkRoute(LinkPath route, Vector3 from)
    {
        var points = route.points;
        bool backwards = (from - points[points.Length - 1]).sqrMagnitude < (from - points[0]).sqrMagnitude;
        float speed = Mathf.Max(agent.speed, 1.2f);
        agent.updateRotation = false;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 target = points[backwards ? points.Length - 1 - i : i] + Vector3.up * agent.baseOffset;
            Vector3 flat = target - transform.position; flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat);
            while ((transform.position - target).sqrMagnitude > 0.0004f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }
    }

    IEnumerator Jump(Vector3 to)
    {
        Vector3 start = transform.position;
        Vector3 end = to + Vector3.up * agent.baseOffset;
        Vector3 flat = end - start; flat.y = 0f;
        agent.updateRotation = false;
        if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat);
        float duration = Mathf.Max(0.35f, Vector3.Distance(start, end) / leapSpeed);
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            // Straight line from start to end, lifted by a hump that peaks halfway.
            transform.position = Vector3.Lerp(start, end, t) + Vector3.up * (leapHeight * 4f * t * (1f - t));
            yield return null;
        }
    }

    void StopLeap()
    {
        StopAllCoroutines();
        leaping = false;
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
        if (Current != Mood.Repelled && Current != Mood.Attack && Current != Mood.Reveal && Current != Mood.Retreat && Current != Mood.BreakIn) agent.updateRotation = true;
        if (Current == Mood.Attack || Current == Mood.BreakIn) return;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, agent.velocity.magnitude, 1f - Mathf.Exp(-8f * Time.deltaTime));
        float speed = smoothedSpeed;
        // Two thresholds, so hovering around one speed doesn't keep swapping walk and run.
        bool run = playing == "Run" ? speed > 2.0f : speed > 2.6f;
        if (run) { Play("Run"); animator.speed = Mathf.Clamp(speed / clipRunSpeed, minRunPlayback, maxRunPlayback); }
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
