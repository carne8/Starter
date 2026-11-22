namespace Starter.Spotify

open System

type Secret =
    { AccessToken: string
      RefreshToken: string
      ExpirationDate: DateTimeOffset }
