export interface IRegisterUserRequestBody {
    firstName :string,
    lastName: string,
    email: string,
    password: string,
    confirmPassword: string
}

export interface ISignInRequestBody {
    email: string,
    password: string
}

export interface ISignInResponseBody {
    message: string,
    token?: string
}


