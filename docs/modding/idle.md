# Idle behaviors

## Idle NPC definitions

In this file you can define which idle activities is declared for an NPC companion.

**Format**

```js
{
  "<NPC_name>": "<behaviors>/<tendencies>/<duration range>"
}
```

| Position | Parameter      | Type         | Description                                                                                                                                                     |
| -------- | -------------- | ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 0        | behaviors      | string[]     | List of behaviors delimited by space, which companion can to do. This behaviors is defined in `Data/IdleBehaviors`                                              |
| 1        | tendencies     | int[]        | List of tendencies which describes how tendency has companion to do this behavior. Index (position) of tendency cresponds to index (position) of behavior.      |
| 2        | duration range | int[2]       | Min duration and max duration of doing current behavior. Total duration is generated randomly between this numbers.                                             |

**IMPORTANT:** *Count of behaviors = count of tendencies*

If count of behaviors are not equal to count of tendencies, then mod crashes!

### Choosing a behavior.

After generated duration timed out, then game try to change behavior. When is choosed the same behavior as current, then current behavior gives a poke event. Poke event is a signal for current behavior. Every behavior kind handles this signal itself. For example: Kind of animation behavior try to change current animation when got poke signal.

![Choose behavior flow](../images/behavior-flow.svg)

**Example**

```js
{
  "Abigail": "Abigail_animate Abigail_lookaround/5 2/10 30",
  "Alex": "Alex_animate Alex_lookaround/2.5 2.5/10 30",
  "Elliott": "Elliott_animate Elliott_lookaround/3 3/10 30",
  "Emily": "Emily_animate Emily_lookaround/3 3/10 30",
  "Haley": "Haley_animate Haley_lookaround/4 2/10 30",
  "Harvey": "Harvey_animate Harvey_lookaround/5 3/10 30",
  "Leah": "Leah_animate Leah_lookaround/3 2.5/10 30",
  "Maru": "Maru_lookaround everybody_idle/2 1/10 30",
  "Penny": "Penny_animate Penny_lookaround/7 1.5/10 30",
  "Sam": "Sam_animate Sam_lookaround/3 3/10 30",
  "Sebastian": "Sebastian_animate Sebastian_lookaround/2 4.5/10 30",
  "Shane": "Shane_animate Shane_lookaround/7 1.5/10 30"
}
```