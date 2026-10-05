# CI/CD Plan — CPSC-491 Project Group 8

**Project:** Unity 6 (6000.6.0f1) 2D top-down dungeon crawler
**Repository:** `rkalkan-a/CPSC-491-Project-Group-8`
**Pipeline definition:** `.github/workflows/main.yml`
**Document version:** Sprint 2 final — 2 October 2026

---

## 1. Purpose

The pipeline exists to stop broken code from reaching `main`. With four people committing to one Unity project, the failure we care most about is the silent one: a change that compiles fine on the author's machine but breaks a system nobody thought to re-test by hand. Unity projects are especially prone to this because so much behaviour lives in scenes, prefabs and physics settings rather than in code that a compiler can check.

Our answer is to run the full automated test suite on every pull request before it can be merged, and again after it lands on `main`, with the results posted back to the pull request where a reviewer will actually see them.

## 2. Platform and tooling

The pipeline runs on **GitHub Actions**, using `ubuntu-latest` hosted runners. Unity itself runs inside Docker via the **GameCI** action suite — `game-ci/unity-test-runner@v4` for tests and `game-ci/unity-builder@v4` for builds — pinned to the image `unityci/editor:ubuntu-6000.6.0f1-base-3`.

GitHub Actions was chosen because the repository already lives on GitHub, so there is no second service to administer, no extra accounts for team members, and the results appear directly on the pull request. GameCI was chosen because it handles Unity's licence activation and headless invocation, which are the two genuinely difficult parts of running Unity on a build server. The `base` image variant is used deliberately: it is the smallest of the GameCI editor images, which matters for runner disk space (see section 9).

The Unity version is declared once as a workflow-level environment variable, `UNITY_VERSION: 6000.6.0f1`, and referenced by both jobs so the test environment and the build environment can never drift apart.

## 3. Source control policy

`main` is the integration branch and is never committed to directly. All work happens on a feature branch named for its purpose — `feature/` for gameplay work, `ci/` for pipeline work, `fix/` for defect repair — and reaches `main` only through a pull request that another group member has reviewed. The pipeline is the automated half of that gate; the human reviewer is the other half.

This matters for the pipeline design: because every change arrives through a pull request, running tests on the `pull_request` event is sufficient to cover all code entering `main`, and the post-merge run on `main` serves as a second confirmation against interaction effects between pull requests that were in flight simultaneously.

## 4. Pipeline triggers

The workflow runs in three situations. On a **pull request targeting `main`**, it runs the test job so the author and reviewer both see a pass or fail before merging. On a **push to `main`**, which in practice means a merge, it runs the test job and then the build job. It can also be started manually through **`workflow_dispatch`**, which is used when debugging the pipeline itself without needing to invent a code change to trigger it.

Earlier in the sprint the workflow only ran on push to `main`. That meant a pull request could be reviewed and merged with no automated verification, and a failure would only surface afterward, once the bad code was already in the integration branch. Adding the `pull_request` trigger moved the check from reporting a problem to preventing one.

## 5. Stage one — automated tests

The `test` job is the gate. It frees disk space on the runner, checks out the repository, restores a cached Unity `Library` folder, runs the full test suite through GameCI, and uploads the results.

The `Library` cache deserves a note. Unity regenerates a large intermediate folder on first import, and rebuilding it from scratch adds several minutes to every run. The cache key is a hash of `Assets/`, `Packages/` and `ProjectSettings/`, with a `restore-keys` fallback so that even when the exact key misses, a recent near-match is reused and only the changed assets are reimported.

Tests run with `testMode: all`, which executes both EditMode and PlayMode suites in a single invocation. This was already the setting when PlayMode tests were introduced this sprint, so no pipeline change was required to pick them up.

## 6. Stage two — build

The `build` job compiles a StandaloneOSX player through `game-ci/unity-builder@v4` and uploads it as a run artifact. It declares `needs: test`, so it only starts if the test job passed — a build of known-broken code has no value.

The build is deliberately skipped on pull requests, via the condition `if: github.event_name != 'pull_request'`. A pull request needs a correctness verdict, and that is what the test job provides; the build is the slowest and most disk-hungry stage in the pipeline, and running it on every push to every open branch spends a great deal of CI time to answer a question nobody asked. It still runs on every merge to `main`, so there is always a current artifact for the integration branch.

Versioning uses `versioning: Custom` with `version: 0.0.${{ github.run_number }}`, which stamps the CI build without modifying `ProjectSettings` in the repository. This keeps builds individually identifiable without generating a spurious file change on every run that someone would then have to decide whether to commit.

## 7. What the tests cover

As of the end of Sprint 2 the suite contains thirty tests across five files.

| Suite | Mode | Tests | Covers |
|---|---|---|---|
| `ProjectSmokeTests` | EditMode | 4 | Build settings list the right scenes, those scene files exist, `Level_01` contains a player, camera and pause menu, the PauseMenu prefab has its script |
| `ProjectileTests` | PlayMode | 8 | Projectile damage amount, lethal damage clamping at zero, destruction on hitting an enemy, on hitting a wall and on reaching max range, distance travelled, owner immunity, pass-through on non-solid triggers |
| `AudioTests` | PlayMode | 7 | Audio system behaviour |
| `MainMenuTests` | PlayMode | 5 | Main menu behaviour |
| `PauseMenuTests` | PlayMode | 6 | Pause menu behaviour |

The PlayMode fixtures are built to mirror how the game actually constructs objects rather than how it would be convenient to construct them in a test. Enemies in `ProjectileTests` are given a Dynamic `Rigidbody2D` because that is what `EnemyBuilder` creates; walls are given a bare `BoxCollider2D` with no rigidbody because that is what `DungeonBootstrapper` creates. These are not interchangeable — Unity's 2D physics treats a static collider differently from one backed by a dynamic body — so a test fixture that simplified the distinction away would pass while telling us nothing about the real game.

## 8. Assembly structure

Running PlayMode tests against gameplay code required a structural change to the project. All of our scripts originally lived in Unity's default `Assembly-CSharp`, and Unity does not permit an assembly definition to reference a predefined assembly. The practical effect was that a test could not refer to `Projectile` or `Health` by type at all — our first EditMode tests had to reach the `PauseMenu` component by string name to work around it.

Three assembly definitions were added to resolve this: `Game.Runtime` over `Assets/_Game/Scripts`, `Game.Editor` over `Assets/_Game/Editor`, and `Game.Tests.PlayMode` over the PlayMode test folder, which references `Game.Runtime` and the test framework. Tests can now use gameplay types directly. This was a prerequisite for the projectile tests specifically, but it unblocked testing generally — the audio and menu suites added later in the sprint sit on the same structure.

## 9. Credentials and secrets

Unity requires licence activation to run headlessly. Three repository secrets supply this: `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD`. They are referenced through the `secrets` context and are never written into the workflow file, logs or the repository. Because all pull requests originate from branches in this repository rather than from forks, secrets are available to pull request runs, which is what allows tests to run before merge.

The workflow also declares an explicit permission scope:

```yaml
permissions:
  contents: read
  checks: write
```

`contents: read` is the minimum needed to check out the code. `checks: write` allows the test runner to create the check run that carries the results. Both are stated explicitly rather than inherited, following least privilege — the token cannot write to the repository contents even if an action attempted to.

## 10. Reporting and artifacts

Results surface in two places. GameCI posts a check run named "Test Results" against the commit, which appears directly in the pull request's checks section with a pass or fail count — this is what a reviewer sees without leaving the page. In parallel, the raw NUnit XML is uploaded as a run artifact with `if: always()`, so the detailed per-test output is retrievable even when the job fails. Build output is uploaded the same way from the build job.

A `df -h /` step runs after disk cleanup in both jobs so that available space is visible in the log, which makes disk exhaustion diagnosable from the run output rather than by guesswork.

## 11. Issues encountered and resolved

Three pipeline defects were found and fixed during Sprint 2, and they are worth recording because each one produced a misleading symptom.

**Tests were not gating merges.** The workflow ran only on push to `main`, so pull requests merged without verification and failures appeared after the fact. Resolved by adding the `pull_request` trigger.

**A green test run reported as a failure.** A run showed 8/8 PlayMode and 12/12 overall passing, then failed with `Resource not accessible by integration` when the test runner attempted to create its check run. The workflow token lacked `checks: write`. The symptom — a red X on the pull request — looked exactly like a test failure and was not one. Resolved by adding the explicit `permissions` block.

**Docker ran out of disk unpacking the Unity image.** Runs failed with `no space left on device` partway through the image pull. The GameCI editor image unpacks to roughly 15GB, which does not fit alongside everything a stock runner preinstalls. The `jlumbroso/free-disk-space` step was present but configured with `tool-cache: false`, which switched off the single largest reclaim available — roughly 8–10GB of preinstalled toolchains the pipeline never uses. Resolved by enabling it and stating the remaining reclaims explicitly. *(Submitted as a pull request; pending review at time of writing.)*

## 12. Known limitations and next steps

The pipeline builds only for StandaloneOSX. Adding Windows to the build matrix would be straightforward but increases run time and disk pressure, so it has been deferred until there is a reason to distribute a Windows build.

Branch protection is not yet enforced on GitHub. The requirement that `main` only receives reviewed pull requests is currently a team agreement rather than a rule the platform enforces, which means nothing technically prevents a direct push. Enabling branch protection with "require status checks to pass" would make the pipeline a hard gate rather than an advisory one, and is the single highest-value improvement available.

Test coverage is concentrated in combat, audio and menus. Room generation, chest and loot behaviour, and door and transition logic are currently unverified, and are the natural targets for the next sprint.

Finally, disk headroom on the runner remains tight even after cleanup. If it becomes a problem again, the next lever is dropping the `Library` cache restore from the test job, trading several minutes of run time for several gigabytes of disk.

---

## Appendix — current workflow definition

The authoritative version is `.github/workflows/main.yml` in the repository; this appendix reflects its state at the time of writing.

```yaml
name: Unity CI

on:
  push:
    branches: [main]
  # combat tests must also run while a PR is open, not just after it merges
  pull_request:
    branches: [main]
  workflow_dispatch:

# the test runner posts a "Test Results" check on the commit
permissions:
  contents: read
  checks: write

env:
  UNITY_VERSION: 6000.6.0f1

jobs:
  test:
    name: Run Unity tests
    runs-on: ubuntu-latest
    steps:
      # the Unity editor image unpacks to roughly 15GB, which does not fit on a
      # stock runner. strip everything the runner preinstalls that we never use.
      - uses: jlumbroso/free-disk-space@v1.3.1
        with:
          tool-cache: true
          android: true
          dotnet: true
          haskell: true
          large-packages: true
          docker-images: true
          swap-storage: true

      - name: Report free disk space
        run: df -h /

      - uses: actions/checkout@v4

      - uses: actions/cache@v4
        with:
          path: Library
          key: Library-test-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: Library-test-

      - uses: game-ci/unity-test-runner@v4
        id: tests
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          unityVersion: ${{ env.UNITY_VERSION }}
          customImage: unityci/editor:ubuntu-6000.6.0f1-base-3
          testMode: all

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: TestResults-${{ github.run_number }}
          path: ${{ steps.tests.outputs.artifactsPath }}

  build:
    name: Build (${{ matrix.targetPlatform }})
    needs: test
    # PRs only need the test gate; the full build runs once the code is on main
    if: github.event_name != 'pull_request'
    runs-on: ubuntu-latest
    strategy:
      matrix:
        targetPlatform: [StandaloneOSX]
    steps:
      - uses: jlumbroso/free-disk-space@v1.3.1
        with:
          tool-cache: true
          android: true
          dotnet: true
          haskell: true
          large-packages: true
          docker-images: true
          swap-storage: true

      - name: Report free disk space
        run: df -h /

      - uses: actions/checkout@v4

      - uses: actions/cache@v4
        with:
          path: Library
          key: Library-${{ matrix.targetPlatform }}-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: Library-${{ matrix.targetPlatform }}-

      # version is only applied to the CI build, ProjectSettings stays untouched
      - uses: game-ci/unity-builder@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          unityVersion: ${{ env.UNITY_VERSION }}
          targetPlatform: ${{ matrix.targetPlatform }}
          buildName: CPSC491-Group8
          versioning: Custom
          version: 0.0.${{ github.run_number }}

      - uses: actions/upload-artifact@v4
        with:
          name: UnityBuild-${{ github.run_number }}
          path: build
```
