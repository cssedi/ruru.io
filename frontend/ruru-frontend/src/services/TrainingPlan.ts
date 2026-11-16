import type { IPlanReadiness, ITrainingPlan } from '../models/TrainingPlan'
import { deleteJson, getJson, postJson } from './http'

export function GetPlanReadiness(): Promise<IPlanReadiness> {
  return getJson<IPlanReadiness>('/trainingplan/readiness')
}

export function GiveAiConsent(version: string): Promise<IPlanReadiness> {
  return postJson<IPlanReadiness>('/trainingplan/consent', { version })
}

export function WithdrawAiConsent(): Promise<IPlanReadiness> {
  return deleteJson<IPlanReadiness>('/trainingplan/consent')
}

/** Creates a draft plan with AI. Takes up to a minute or two. */
export function GeneratePlan(): Promise<ITrainingPlan> {
  return postJson<ITrainingPlan>('/trainingplan/generate')
}

/** The saved plan, or null when there isn't one (the API answers 204). */
export function GetCurrentPlan(): Promise<ITrainingPlan | null> {
  return getJson<ITrainingPlan | null>('/trainingplan/current')
}

export function SavePlan(id: string): Promise<ITrainingPlan> {
  return postJson<ITrainingPlan>(`/trainingplan/${id}/save`)
}

export function DiscardPlan(id: string): Promise<null> {
  return deleteJson<null>(`/trainingplan/${id}`)
}
