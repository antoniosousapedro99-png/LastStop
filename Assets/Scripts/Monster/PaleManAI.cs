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
// He walks on the underground navigation mesh only, so the street is safe.
[RequireComponent(typeof(NavMeshAgent), typeof(PaleManSenses))]
public class PaleManAI : MonoBehaviour
{
    public enum Mood { Wander, Investigate, Chase, Search, Attack, Retreat, Repelled }

    public Animator animator;
    public float walkSpeed = 1.4f, chaseSpeed = 3.8f;
    public float attackRange = 1.6f, hitReach = 2.1f;
    public float loseAfter = 8f;               // seconds without seeing the player before giving up the chase
    public float flareKeepAway = 6f;           // metres he stays from a lit flare
    public float clipWalkSpeed = 1.2f, clipRunSpeed = 3.4f;   // how fast the animations' feet move, to match them up

    [Header("Sounds")]
    public AudioSource voice;                  // breathing, shrieks
    public AudioSource feet;
    public AudioClip[] breaths, shrieks, steps;
    public AudioClip hitSound;

    public Mood Current { get; private set; } = Mood.Wander;

    NavMeshAgent agent;
    PaleManSenses senses;
    float moodTime, lastSeen, nextBreath, stepTimer, sightTimer;
    Vector3 lastSeenAt;
    bool hitDone, seesPlayer;
    string playing;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        senses = GetComponent<PaleManSenses>();
    }

    void OnEnable() => SetMood(Mood.Wander);

    float screamUntil;

    // Used by the arrival: he stands and shrieks before he starts to hunt.
    public void WakeWithScream()
    {
        screamUntil = Time.time + 1.4f;
        Play("Scream");   // the shriek itself comes from PaleManArrival's far-carrying speaker
    }

    void Update()
    {
        if (Time.time < screamUntil) { agent.isStopped = true; return; }
        moodTime += Time.deltaTime;
        sightTimer -= Time.deltaTime;
        if (sightTimer <= 0f) { sightTimer = 0.2f; seesPlayer = senses.CanSeePlayer(); }
        if (seesPlayer) { lastSeen = Time.time; lastSeenAt = senses.Player.position; }

        // A lit flare close by overrides everything else.
        var flare = Flare.NearestLit(transform.position);
        if (flare && Current != Mood.Repelled && Vector3.Distance(flare.position, transform.position) < flareKeepAway)
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
        }
        if (Current != Mood.Attack && agent.isStopped && !Arrived()) agent.isStopped = false;   // after a scream or a look-around
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
        if (Current != Mood.Repelled && Current != Mood.Attack) agent.updateRotation = true;
        if (Current == Mood.Attack) return;
        float speed = agent.velocity.magnitude;
        if (speed > 2.4f) { Play("Run"); animator.speed = speed / clipRunSpeed; }
        else if (speed > 0.2f) { Play("Walk"); animator.speed = Mathf.Max(0.5f, speed / clipWalkSpeed); }
        else { Play("Idle"); animator.speed = 1f; }
    }

    public void Play(string state)
    {
        if (playing == state) return;
        playing = state;
        if (state == "Attack" || state == "Scream") animator.speed = 1f;
        animator.CrossFadeInFixedTime(state, 0.2f);
    }

    void Sounds()
    {
        // Slow, heavy steps that match his pace.
        float speed = agent.velocity.magnitude;
        if (speed > 0.2f && steps.Length > 0)
        {
            stepTimer -= Time.deltaTime * speed;
            if (stepTimer <= 0f) { stepTimer = 1.3f; feet.pitch = Random.Range(0.65f, 0.75f); feet.PlayOneShot(steps[Random.Range(0, steps.Length)], 0.9f); }
        }
        // Wet, low breathing every few seconds.
        if (Time.time > nextBreath && breaths.Length > 0)
        {
            nextBreath = Time.time + Random.Range(3f, 6f);
            Say(breaths[Random.Range(0, breaths.Length)], 0.7f);
        }
    }

    void Say(AudioClip clip, float volume)
    {
        voice.pitch = Random.Range(0.5f, 0.6f);   // far below a human voice
        voice.PlayOneShot(clip, volume);
    }

    // Used by the arrival: the scream when he first wakes.
    public void Shriek()
    {
        if (shrieks.Length > 0) Say(shrieks[Random.Range(0, shrieks.Length)], 1f);
    }
}
