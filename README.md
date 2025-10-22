# Conway's Game of Life
My attempt to unlock the power of Unity DOTS starting from rules as simple as fascinating!

This is a submission for [Unity DOTS Community Challenge #1](https://itch.io/jam/dots-challenge-1).

## Process
The journey began from the conventional implementations of Conway’s Game of Life [algorithm](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life#Algorithms), which typically employ two arrays and a texture for their operation. However, given the capabilities of DOTS and its previously analyzed applications, I decided to treat each cell as an individual entity.

The initial phase involved setting up the basic spawning and rendering mechanisms. I created a prefab with a cube representing each cell, which were then spawned in a grid pattern. The current state of each cell was randomized and visually represented by altering the base color of the material - black signified a dead cell, while green indicated a living one.

I experimented with an enableable component, but quickly realized that each cell required an update regardless. I had read about an optimization technique that ignores inactive areas, but it seemed premature to implement it at this stage. Consequently, I decided to simply store two boolean values in the Cell component, representing the current and next states of the cell.

To prevent the Unity editor from slowing down excessively or freezing due to the execution of unoptimized code, I introduced a delay between simulation ticks. This made the subsequent steps of the process more manageable.

The next challenge was figuring out how to access neighbouring cells. I concluded that the most efficient method would be to add a buffer component containing a list of neighbouring entities, which would be computed during the cell spawn process.

Around the same time, I also decided to go for the toroidal approach, thereby creating an infinite grid without the need for different behaviours along the edges.

Once these elements were in place, the simulation began to work as expected. I ensured that all possible methods were Burst compiled. However, running the simulation system code on the main thread significantly hampered performance. To address this, I converted the code to run within an IJobEntity job. I then disabled the job safety parallel access restriction, as the cell component is read by all neighbours but only written in its iteration. These modifications reduced the CPU time for the simulation system from 77 ms to 2-3 ms for the number of entities I was profiling.

At this point, the drawing system became the bottleneck. Converting its update into a job and scheduling it for parallel execution was a straightforward task.

After refining the code, I divided the simulation and drawing systems into more systems, one for each optimization level, for easier comparison later on. I also added all the necessary conditions to run them alternately, as demonstrated in the Unity samples.

Inspired by the official advanced tutorial for [advanced optimization in DOTS](https://learn.unity.com/project/part-3-implementation-and-optimization), I attempted to further enhance the performance of my implementation. Some of the strategies I tried included flattening the iteration over neighbours with direct index accesses (since there are always eight of them) and linearizing code jumps by replacing conditions with calculations and converting booleans into integers to fit these calculations (for example, multiplying color with state instead of using state to choose between two colors). However, these modifications did not yield any noticeable improvements and only served to complicate the code, so I ultimately decided to discard this step.

Finally, I implemented a user interface to interact with parameters in the build. It turned out not to be a trivial part of the project, as I faced both the complications of the interaction between entities and GameObjects and the implication of using subscenes and streamed content, given that until that moment my entire implementation exclusively used entities.

I would have loved to experiment more with the rendering aspects, but due to the limited available time to devote to the project i ended up setting an orthographic camera and unlit materials, reaching about the same result I would have achieved just using a texture.

## Metrics
Laptop:
- CPU Intel Core i7-10875H (16 cores)
- GPU NVIDIA GeForce RTX 2070
- RAM 32 GB

| Execute             | 60 fps  | 30 fps    |
| ------------------- | ------: | --------: |
| Main Thread         | 190,000 | 390,000   |
| Single Threaded Job | 260,000 | 520,000   |
| Parallel Job        | 520,000 | 1,000,000 |

## Considerations
I've been learning DOTS for a relatively short time, following Turbo Make Games, Code Monkey and Unity's resources and webinars. This was my first DOTS project started from scratch, without following any tutorial, and it proved to be a valuable learning experience.
