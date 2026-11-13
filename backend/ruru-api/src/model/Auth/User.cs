using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using ruru_api.DTOs.Auth;

namespace ruru_api.model;

public class User : IdentityUser<Guid>
{
    [Required]
    [StringLength(25)]
    public string FirstName {get; init;}
    [Required]
    [StringLength(25)]
    public string LastName {get; init;}
    public ICollection<Workout> Workouts {get; init;} = new List<Workout>();
    public UserProfile? Profile { get; set; }

}
