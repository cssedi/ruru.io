import type { IRegisterUserRequestBody, ISignInRequestBody, ISignInResponseBody } from '../models/Auth'
import { postJson } from './http'


export function RegisterAccount(body: IRegisterUserRequestBody): Promise<ISignInResponseBody> {
  return postJson<ISignInResponseBody, IRegisterUserRequestBody>('/auth/register', body)
}

export function SignIn(body: ISignInRequestBody): Promise<ISignInResponseBody> {
  return postJson<ISignInResponseBody, ISignInRequestBody>('/auth/login', body)
}