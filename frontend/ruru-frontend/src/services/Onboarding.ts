import type { IGiveConsentRequestBody, IOnboardingAnswers, IOnboardingStatus } from '../models/Onboarding'
import { deleteJson, getJson, postJson, putJson } from './http'

export function GetOnboardingStatus(): Promise<IOnboardingStatus> {
  return getJson<IOnboardingStatus>('/onboarding')
}

/** Saves progress. Replaces every stored answer, so always send the full set. */
export function SaveOnboardingAnswers(answers: IOnboardingAnswers): Promise<IOnboardingStatus> {
  return putJson<IOnboardingStatus, IOnboardingAnswers>('/onboarding', answers)
}

export function CompleteOnboarding(): Promise<IOnboardingStatus> {
  return postJson<IOnboardingStatus>('/onboarding/complete')
}

/** "Not right now": finishes onboarding without saving any health details. */
export function SkipOnboarding(): Promise<IOnboardingStatus> {
  return postJson<IOnboardingStatus>('/onboarding/skip')
}

export function GiveConsent(version: string): Promise<IOnboardingStatus> {
  return postJson<IOnboardingStatus, IGiveConsentRequestBody>('/onboarding/consent', { version })
}

/** Withdraws consent; the API deletes every stored answer. */
export function WithdrawConsent(): Promise<IOnboardingStatus> {
  return deleteJson<IOnboardingStatus>('/onboarding/consent')
}
