import request from './request'

export const authApi = {
  login: (data) => request.post('/v1/auth/login', data),
  getLoginOptions: () => request.get('/v1/auth/login-options'),
  me: () => request.get('/v1/auth/me'),
  logout: () => request.post('/v1/auth/logout')
}
