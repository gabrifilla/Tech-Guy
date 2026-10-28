"""Aggregate V2 harness outputs; write aggregate files without running Unity.

Run only after the three Unity cases have passed:
  python Tools/BoonValidation/summarize_boon_route.py
"""

import argparse
import csv
import json
from collections import Counter
from pathlib import Path


def read_csv(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def boolean(value):
    return value.lower() == "true"


def ratio(numerator, denominator):
    return {"numerator": numerator, "denominator": denominator,
            "observed_fraction": numerator / denominator if denominator else None}


def final_records(build):
    _, _, content = build.partition(":")
    result = {}
    for token in content.split("|"):
        if not token:
            continue
        name, _, target_rank = token.partition("@")
        target, _, rank = target_rank.rpartition(":")
        result[name] = {"target": target, "rank": int(rank)}
    return result


def summarize(directory):
    report = {
        "method": "CSV aggregation of passed Unity V2 cases. 300 seeds/weapon/mode; 75/path; uniform independent card choice.",
        "limitations": [
            "Assumes all route encounters are won; no gameplay survival model.",
            "Four paths are equally stratified, not an estimate of player path preference.",
            "Reward-count prefixes measure economy; they are not elapsed-time estimates or longer hypothetical runs.",
            "Production weights2:1 and uniform counterfactual share run/choice seeds, but choices and conditional eligibility diverge.",
            "Unique signatures/profile presence do not prove human-recognizable or balanced archetypes.",
            "No new opportunities, weights, pity guarantees or runtime content have been introduced.",
        ],
        "groups": [],
    }
    for weapon in ("Gauntlet", "Bow", "Spear"):
        rows = {suffix: read_csv(directory / f"{weapon}-{suffix}.csv") for suffix in
                ("runs", "cards", "opportunities", "prerequisites", "prefixes")}
        progress = json.loads((directory / f"{weapon}-progress.json").read_text(encoding="utf-8-sig"))
        assert progress["state"] == "complete" and progress["completedRuns"] == 600, progress
        # Prerequisite IDs come from the harness's runtime-derived records.
        actual_duos = sorted({row["duo"] for row in rows["prerequisites"]})
        for mode in ("production_2_to_1", "counterfactual_uniform"):
            selected = {suffix: [row for row in data if row["mode"] == mode] for suffix, data in rows.items()}
            runs = selected["runs"]
            assert len(runs) == 300, (weapon, mode, len(runs))
            assert Counter(row["path"] for row in runs) == Counter({str(i): 75 for i in range(4)})
            cards = selected["cards"]
            opportunities = selected["opportunities"]
            prerequisites = selected["prerequisites"]
            by_opportunity = {(row["seed"], row["opportunity"]): row for row in opportunities}
            acquisitions = [row for row in cards if row["stage"] == "acquired"]
            records = [final_records(row["final_build"]) for row in runs]
            families = {row["id"]: row["family"] for row in cards}
            assert set(families.values()) <= {"Mutation", "Augment", "Protocol", "Resonance", "Continue"}
            group = {
                "weapon": weapon, "mode": mode, "runs": len(runs),
                "unique_final_builds": len({row["final_build"] for row in runs}),
                "histograms": {field: dict(sorted(Counter(int(row[field]) for row in runs).items())) for field in
                               ("opportunities", "technique_opportunities", "protocol_opportunities", "mutation_opportunities", "first_mutation")},
                "total_alternative_tickets": sum(int(row["alternative_tickets"]) for row in runs),
                "total_continue_picks": sum(int(row["continue_picks"]) for row in runs),
                "total_repeated_card_sets": sum(int(row["repeated_card_sets"]) for row in runs),
                "mutation_valid_opportunities": ratio(sum(row["announced_kind"] == "Mutation" and bool(row["eligible_ids"]) for row in opportunities),
                                                       sum(row["announced_kind"] == "Mutation" for row in opportunities)),
                "four_families_final_runs": sum({"Mutation", "Augment", "Protocol", "Resonance"}.issubset(
                    {families.get(name) for name in build}) for build in records),
                "cards": [], "duos": [], "slots": [], "paths": [], "prefixes": [],
            }
            for card_id in sorted(families):
                subset = [row for row in cards if row["id"] == card_id]
                item = {"id": card_id, "family": families[card_id]}
                for stage in ("eligible", "offered", "acquired"):
                    stage_rows = [row for row in subset if row["stage"] == stage]
                    item[stage + "_occurrences"] = len(stage_rows)
                    item[stage + "_runs"] = len({row["seed"] for row in stage_rows})
                item["acquired_targets"] = dict(Counter(row["targets_if_acquired"] for row in subset if row["stage"] == "acquired"))
                group["cards"].append(item)
            for duo in actual_duos:
                samples = [row for row in prerequisites if row["duo"] == duo]
                completed = {row["seed"] for row in samples if not row["missing_after"]}
                eligible = {row["seed"] for row in samples if boolean(row["eligible_now"])}
                offered = {row["seed"] for row in samples if boolean(row["offered_now"])}
                acquired = {row["seed"] for row in samples if boolean(row["acquired_now"])}
                first_complete = {}
                for row in samples:
                    if not row["missing_after"] and row["seed"] not in first_complete:
                        remaining = int(row["remaining_techniques_including_now"])
                        opportunity_row = by_opportunity[(row["seed"], row["opportunity"])]
                        if opportunity_row["announced_kind"] == "Technique":
                            remaining -= 1
                        first_complete[row["seed"]] = remaining
                group["duos"].append({
                    "id": duo, "required_ids": samples[0]["required_ids"].split("|") if samples else [],
                    "prerequisites_completed_runs": len(completed), "eligible_runs": len(eligible), "offered_runs": len(offered), "acquired_runs": len(acquired),
                    "offered_given_eligible": ratio(len(offered), len(eligible)),
                    "acquired_given_prerequisites": ratio(len(acquired), len(completed)),
                    "acquired_given_offered": ratio(len(acquired), len(offered)),
                    "techniques_remaining_at_first_prerequisite_completion": dict(sorted(Counter(first_complete.values()).items())),
                    "missing_prerequisite_observations": dict(Counter(prerequisite for row in samples for prerequisite in row["missing_before"].split("|") if prerequisite)),
                })
            for slot in ("Q", "W", "E", "R"):
                slot_cards = [row for row in acquisitions if slot in [value.strip() for value in row["targets_if_acquired"].split(",")]]
                group["slots"].append({"slot": slot, "mutation_picks": dict(Counter(row["id"] for row in slot_cards if row["family"] == "Mutation")),
                                       "augment_picks": dict(Counter(row["id"] for row in slot_cards if row["family"] == "Augment")),
                                       "duo_picks": dict(Counter(row["id"] for row in slot_cards if row["family"] == "Resonance"))})
            for path in range(4):
                subset = [row for row in runs if int(row["path"]) == path]
                group["paths"].append({"path": path, "labels": subset[0]["path_labels"], "runs": len(subset),
                    "unique_final_builds": len({row["final_build"] for row in subset}),
                    "opportunities_histogram": dict(sorted(Counter(int(row["opportunities"]) for row in subset).items())),
                    "runs_with_duo": sum(int(row["duo_picks"]) > 0 for row in subset)})
            for count in sorted({int(row["opportunities"]) for row in selected["prefixes"]}):
                subset = [row for row in selected["prefixes"] if int(row["opportunities"]) == count]
                group["prefixes"].append({"reward_count": count, "runs_observed": len(subset),
                    "unique_builds": len({row["build"] for row in subset}), "runs_with_mutation": sum(int(row["mutation_count"]) > 0 for row in subset),
                    "runs_with_duo": sum(int(row["duo_count"]) > 0 for row in subset),
                    "four_families_runs": sum(int(row["mutation_count"]) > 0 and int(row["augment_count"]) > 0 and int(row["protocol_acquisitions"]) > 0 and int(row["duo_count"]) > 0 for row in subset)})
            report["groups"].append(group)
    return report


def markdown(report):
    text = ["# V2 — funil da rota publicada", "", "Dados da simulação em Unity; não medem aceite humano, sobrevivência ou duração em segundos.", "",
            "| Arma | Modo | Runs | Builds únicas | Oportunidades | Mutation | Runs com Duo | Mutation+Augment+Protocol+Duo |",
            "| --- | --- | ---: | ---: | --- | --- | ---: | ---: |"]
    for group in report["groups"]:
        histogram = group["histograms"]["opportunities"]
        duo_count = sum(path["runs_with_duo"] for path in group["paths"])
        text.append(f"| {group['weapon']} | {group['mode']} | {group['runs']} | {group['unique_final_builds']} | {min(histogram)}–{max(histogram)} | {group['histograms']['mutation_opportunities']} | {duo_count} | {group['four_families_final_runs']} |")
    text += ["", "| Arma | Modo | Duo | Pré-requisitos completos | Elegível | Ofertado | Adquirido |", "| --- | --- | --- | ---: | ---: | ---: | ---: |"]
    for group in report["groups"]:
        for duo in group["duos"]:
            text.append(f"| {group['weapon']} | {group['mode']} | {duo['id']} | {duo['prerequisites_completed_runs']} | {duo['eligible_runs']} | {duo['offered_runs']} | {duo['acquired_runs']} |")
    text += ["", "Limites:", ""] + ["- " + limitation for limitation in report["limitations"]]
    return "\n".join(text) + "\n"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--directory", type=Path, default=Path(__file__).resolve().parents[2] / "Docs" / "validation" / "artifacts" / "boons" / "V2")
    args = parser.parse_args()
    result = summarize(args.directory)
    (args.directory / "V2-aggregate.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    (args.directory / "V2-summary.md").write_text(markdown(result), encoding="utf-8")
    print(args.directory / "V2-summary.md")


if __name__ == "__main__":
    main()
